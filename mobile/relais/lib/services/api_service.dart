import 'dart:convert';
import 'dart:io';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:http/http.dart' as http;
import '../config.dart';

typedef VoidCallback = void Function();

class ApiService {
  final _storage = const FlutterSecureStorage();
  String? _accessToken;
  String? _refreshToken;
  VoidCallback? onSessionExpired;

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
    required String nomRelais,
    required String adresse,
    required String ville,
    required String pays,
  }) async {
    final res = await _post('/api/auth/register/relais', {
      'nomRelais': nomRelais,
      'email': email,
      'telephone': telephone,
      'motDePasse': motDePasse,
      'adresse': adresse,
      'ville': ville,
      'pays': pays,
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
  // KYC
  // ============================================

  Future<Map<String, dynamic>> getKycStatus() async =>
      await _get('/api/kyc/status');

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
  // PROFIL POINT RELAIS
  // ============================================

  Future<Map<String, dynamic>> getProfil() async =>
      await _get('/api/relais/profil');

  Future<Map<String, dynamic>> updateProfil(Map<String, dynamic> data) async =>
      await _put('/api/relais/profil', data);

  // ============================================
  // COLIS POINT RELAIS
  // ============================================

  Future<List<dynamic>> getColisList() async =>
      await _getList('/api/relais/colis');

  Future<Map<String, dynamic>> scanColis(String codeColis) async =>
      await _post('/api/relais/colis/$codeColis/scan', {});

  Future<Map<String, dynamic>> validerPaiementEspeces(String commandeId) async =>
      await _post('/api/relais/paiement/$commandeId/valider-especes', {});

  Future<Map<String, dynamic>> confirmerDepot(String codeColis) async =>
      await _post('/api/relais/colis/$codeColis/confirmer-depot', {});

  Future<Map<String, dynamic>> confirmerRetrait(
      String codeColis, String codeRetrait) async =>
      await _post('/api/relais/colis/$codeColis/confirmer-retrait', {
        'codeRetrait': codeRetrait,
      });

  // ============================================
  // HELPERS
  // ============================================

  Future<void> _handleExpiredSession() async {
    await logout();
    onSessionExpired?.call();
  }

  Future<Map<String, dynamic>> _get(String path) async {
    final res = await http.get(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
    );
    if (res.statusCode == 401) {
      if (await refreshToken()) return _get(path);
      await _handleExpiredSession();
      return {'error': 'Session expirée. Veuillez vous reconnecter.'};
    }
    return _parseJson(res);
  }

  Future<List<dynamic>> _getList(String path) async {
    final res = await http.get(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
    );
    if (res.statusCode == 401) {
      if (await refreshToken()) return _getList(path);
      await _handleExpiredSession();
      return [];
    }
    if (res.statusCode != 200) return [];
    try {
      return jsonDecode(res.body);
    } catch (_) {
      return [];
    }
  }

  Future<Map<String, dynamic>> _post(
      String path, Map<String, dynamic> body) async {
    final res = await http.post(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (res.statusCode == 401) {
      if (await refreshToken()) return _post(path, body);
      await _handleExpiredSession();
      return {'error': 'Session expirée. Veuillez vous reconnecter.'};
    }
    return _parseJson(res);
  }

  Future<Map<String, dynamic>> _put(
      String path, Map<String, dynamic> body) async {
    final res = await http.put(
      Uri.parse('${AppConfig.apiBaseUrl}$path'),
      headers: _headers,
      body: jsonEncode(body),
    );
    if (res.statusCode == 401) {
      if (await refreshToken()) return _put(path, body);
      await _handleExpiredSession();
      return {'error': 'Session expirée. Veuillez vous reconnecter.'};
    }
    return _parseJson(res);
  }

  Map<String, dynamic> _parseJson(http.Response res) {
    if (res.body.trimLeft().startsWith('<')) {
      return {'error': 'Erreur serveur (HTTP ${res.statusCode}). Réessayez.'};
    }
    try {
      return jsonDecode(res.body);
    } catch (_) {
      return {'error': 'Réponse invalide du serveur (HTTP ${res.statusCode}).'};
    }
  }

  Future<void> _saveTokens(String access, String refresh) async {
    _accessToken = access;
    _refreshToken = refresh;
    await _storage.write(key: 'accessToken', value: access);
    await _storage.write(key: 'refreshToken', value: refresh);
  }
}
