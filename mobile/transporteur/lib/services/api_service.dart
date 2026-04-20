import 'dart:convert';
import 'dart:io';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import '../config.dart';

class ApiService {
  final _storage = const FlutterSecureStorage();
  String? _accessToken;
  String? _refreshToken;

  Future<void> init() async {
    _accessToken = await _storage.read(key: 'accessToken');
    _refreshToken = await _storage.read(key: 'refreshToken');
  }

  bool get isLoggedIn => _accessToken != null;

  Map<String, String> get _headers => {
        'Content-Type': 'application/json',
        if (_accessToken != null) 'Authorization': 'Bearer $_accessToken',
      };

  // ============================================
  // AUTH
  // ============================================

  Future<Map<String, dynamic>> login(String email, String password) async {
    final res = await _post('/api/auth/login', {
      'email': email,
      'motDePasse': password,
    });
    if (res.containsKey('accessToken')) {
      await _saveTokens(res['accessToken'], res['refreshToken']);
    }
    return res;
  }

  Future<Map<String, dynamic>> register({
    required String prenom,
    required String nom,
    required String email,
    required String telephone,
    required String motDePasse,
    required String typeVehicule,
    required String corridorsActifs,
  }) async {
    final res = await _post('/api/auth/register/transporteur', {
      'prenom': prenom,
      'nom': nom,
      'email': email,
      'telephone': telephone,
      'motDePasse': motDePasse,
      'confirmationMotDePasse': motDePasse,
      'typeVehicule': typeVehicule,
      'corridorsActifs': corridorsActifs,
    });
    if (res.containsKey('accessToken')) {
      await _saveTokens(res['accessToken'], res['refreshToken']);
    }
    return res;
  }

  Future<void> logout() async {
    _accessToken = null;
    _refreshToken = null;
    await _storage.deleteAll();
  }

  Future<bool> refreshToken() async {
    if (_refreshToken == null) return false;
    try {
      final res = await http.post(
        Uri.parse('${AppConfig.apiBaseUrl}/api/auth/refresh'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'refreshToken': _refreshToken}),
      );
      if (res.statusCode == 200) {
        final data = jsonDecode(res.body);
        await _saveTokens(data['accessToken'], data['refreshToken']);
        return true;
      }
    } catch (_) {}
    return false;
  }

  // ============================================
  // TRAJETS
  // ============================================

  Future<List<dynamic>> getMesTrajets() async => await _getList('/api/trajets');

  Future<Map<String, dynamic>> createTrajet(Map<String, dynamic> data) async =>
      await _post('/api/trajets', data);

  Future<Map<String, dynamic>> updateTrajet(
          String id, Map<String, dynamic> data) async =>
      await _put('/api/trajets/$id', data);

  Future<void> deleteTrajet(String id) async => await _delete('/api/trajets/$id');

  Future<List<dynamic>> getColisForTrajet(String trajetId) async =>
      await _getList('/api/trajets/$trajetId/colis');

  // ============================================
  // KYC
  // ============================================

  Future<Map<String, dynamic>> getKycStatus() async => await _get('/api/kyc/status');

  Future<Map<String, dynamic>> uploadKycDocument(
      String typeDocument, File file) async {
    final uri = Uri.parse('${AppConfig.apiBaseUrl}/api/kyc/upload');
    final request = http.MultipartRequest('POST', uri);
    request.headers['Authorization'] = 'Bearer $_accessToken';
    request.fields['typeDocument'] = typeDocument;
    request.files.add(await http.MultipartFile.fromPath('fichier', file.path));
    final response = await request.send();
    final body = await response.stream.bytesToString();
    return jsonDecode(body);
  }

  // ============================================
  // COMMANDES TRANSPORTEUR
  // ============================================

  Future<List<dynamic>> getMesCommandesTransporteur() async =>
      await _getList('/api/commandes/transporteur');

  // ============================================
  // COLIS
  // ============================================

  Future<Map<String, dynamic>> getColisByCode(String code) async =>
      await _get('/api/colis/$code');

  Future<Map<String, dynamic>> updateStatutColis(
      String code, String statut, String? commentaire,
      {double? lat, double? lng}) async {
    return await _put('/api/colis/$code/statut', {
      'nouveauStatut': statut,
      'commentaire': commentaire,
      if (lat != null) 'latitude': lat,
      if (lng != null) 'longitude': lng,
    });
  }

  Future<Map<String, dynamic>> uploadPhotoColis(
      String code, File photo) async {
    final uri = Uri.parse('${AppConfig.apiBaseUrl}/api/colis/$code/photo');
    final request = http.MultipartRequest('POST', uri);
    request.headers['Authorization'] = 'Bearer $_accessToken';
    request.files.add(await http.MultipartFile.fromPath('photo', photo.path));
    final response = await request.send();
    final body = await response.stream.bytesToString();
    return jsonDecode(body);
  }

  // ============================================
  // COMMANDES (vue transporteur)
  // ============================================

  Future<List<dynamic>> getMesCommandes() async =>
      await _getList('/api/commandes');

  // ============================================
  // HELPERS
  // ============================================

  Future<Map<String, dynamic>> _get(String path) async {
    final res = await http.get(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
    );
    if (res.statusCode == 401 && await refreshToken()) {
      return _get(path);
    }
    return jsonDecode(res.body);
  }

  Future<List<dynamic>> _getList(String path) async {
    final res = await http.get(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
    );
    if (res.statusCode == 401 && await refreshToken()) {
      return _getList(path);
    }
    if (res.statusCode != 200) return [];
    return jsonDecode(res.body);
  }

  Future<Map<String, dynamic>> _post(
      String path, Map<String, dynamic> body) async {
    final res = await http.post(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (res.statusCode == 401 && await refreshToken()) {
      return _post(path, body);
    }
    return jsonDecode(res.body);
  }

  Future<Map<String, dynamic>> _put(
      String path, Map<String, dynamic> body) async {
    final res = await http.put(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (res.statusCode == 401 && await refreshToken()) {
      return _put(path, body);
    }
    return jsonDecode(res.body);
  }

  Future<void> _delete(String path) async {
    final res = await http.delete(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
    );
    if (res.statusCode == 401 && await refreshToken()) {
      return _delete(path);
    }
  }

  Future<void> _saveTokens(String access, String refresh) async {
    _accessToken = access;
    _refreshToken = refresh;
    await _storage.write(key: 'accessToken', value: access);
    await _storage.write(key: 'refreshToken', value: refresh);
  }
}
