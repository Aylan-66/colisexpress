import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class ColisDetailScreen extends StatefulWidget {
  final String codeColis;
  const ColisDetailScreen({super.key, required this.codeColis});

  @override
  State<ColisDetailScreen> createState() => _ColisDetailScreenState();
}

class _ColisDetailScreenState extends State<ColisDetailScreen> {
  Map<String, dynamic>? _colis;
  bool _loading = true;
  String? _error;
  String? _success;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    final res = await context.read<ApiService>().getColisByCode(widget.codeColis);
    setState(() {
      _loading = false;
      if (res.containsKey('error')) {
        _error = res['error'];
      } else {
        _colis = res;
      }
    });
  }

  Future<void> _updateStatut(String statut, String commentaire) async {
    setState(() { _loading = true; _success = null; });
    final api = context.read<ApiService>();
    final res = await api.updateStatutColis(widget.codeColis, statut, commentaire);
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = 'Statut mis à jour : $statut');
      await _load();
    }
  }

  Future<void> _takePhoto() async {
    final picker = ImagePicker();
    final photo = await picker.pickImage(source: ImageSource.camera, maxWidth: 1200, imageQuality: 80);
    if (photo == null) return;

    setState(() { _loading = true; _success = null; });
    final api = context.read<ApiService>();
    final res = await api.uploadPhotoColis(widget.codeColis, File(photo.path));
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = 'Photo enregistrée.');
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.codeColis)),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && _colis == null
              ? Center(child: Text(_error!, style: const TextStyle(color: AppTheme.danger)))
              : _colis == null
                  ? const Center(child: Text('Colis introuvable'))
                  : RefreshIndicator(onRefresh: _load, child: _buildContent()),
    );
  }

  Widget _buildContent() {
    final statut = _colis!['statut']?.toString() ?? '—';
    final evenements = (_colis!['evenements'] as List?) ?? [];

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (_success != null)
          Container(
            padding: const EdgeInsets.all(12),
            margin: const EdgeInsets.only(bottom: 12),
            decoration: BoxDecoration(
              color: AppTheme.success.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(_success!, style: const TextStyle(color: AppTheme.success, fontWeight: FontWeight.w600)),
          ),
        if (_error != null)
          Container(
            padding: const EdgeInsets.all(12),
            margin: const EdgeInsets.only(bottom: 12),
            decoration: BoxDecoration(
              color: AppTheme.danger.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(_error!, style: const TextStyle(color: AppTheme.danger, fontWeight: FontWeight.w600)),
          ),

        // Info card
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text('${_colis!['villeDepart']} → ${_colis!['villeArrivee']}',
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppTheme.primary.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(statut,
                          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppTheme.primary)),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                _infoRow('Destinataire', _colis!['nomDestinataire'] ?? '—'),
                _infoRow('Poids déclaré', '${_colis!['poidsDeclare'] ?? 0} kg'),
                if (_colis!['dimensions'] != null) _infoRow('Dimensions', _colis!['dimensions']),
                _infoRow('Code retrait', _colis!['codeRetrait'] ?? '—'),
              ],
            ),
          ),
        ),

        const SizedBox(height: 12),

        // Actions
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Actions', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: [
                    _actionButton('Prendre en charge', 'ReceptionneParTransporteur', Icons.handshake, AppTheme.primary),
                    _actionButton('Photo prise en charge', null, Icons.camera_alt, AppTheme.accent, onTap: _takePhoto),
                    _actionButton('En transit', 'EnTransit', Icons.local_shipping, AppTheme.primary),
                    _actionButton('Arrivé destination', 'ArriveDansPaysDest', Icons.flag, AppTheme.success),
                    _actionButton('Au point relais', 'ReceptionneParPointRelais', Icons.store, AppTheme.success),
                    _actionButton('Disponible retrait', 'DisponibleAuRetrait', Icons.check_circle, AppTheme.success),
                    _actionButton('Signaler incident', 'Incident', Icons.warning, AppTheme.danger),
                  ],
                ),
              ],
            ),
          ),
        ),

        const SizedBox(height: 12),

        // Timeline
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Timeline', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                const SizedBox(height: 12),
                if (evenements.isEmpty)
                  const Text('Aucun événement', style: TextStyle(color: AppTheme.textMuted))
                else
                  ...evenements.map((e) => _timelineItem(e)),
              ],
            ),
          ),
        ),
      ],
    );
  }

  Widget _actionButton(String label, String? statut, IconData icon, Color color, {VoidCallback? onTap}) {
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        icon: Icon(icon, size: 18, color: color),
        label: Text(label),
        style: OutlinedButton.styleFrom(
          foregroundColor: color,
          side: BorderSide(color: color.withValues(alpha: 0.3)),
          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
        ),
        onPressed: () {
          if (onTap != null) {
            onTap();
            return;
          }
          if (statut != null) _updateStatut(statut, label);
        },
      ),
    );
  }

  Widget _infoRow(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Row(
          children: [
            SizedBox(width: 120, child: Text(label, style: const TextStyle(color: AppTheme.textMuted, fontSize: 13))),
            Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13))),
          ],
        ),
      );

  Widget _timelineItem(Map<String, dynamic> e) {
    final statut = e['nouveauStatut']?.toString() ?? '—';
    final commentaire = e['commentaire']?.toString();
    final date = e['dateHeure']?.toString() ?? '';
    final d = DateTime.tryParse(date);
    final dateStr = d != null ? '${d.day}/${d.month}/${d.year} ${d.hour}:${d.minute.toString().padLeft(2, '0')}' : date;

    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(width: 10, height: 10,
                  decoration: BoxDecoration(shape: BoxShape.circle, color: AppTheme.primary)),
              Container(width: 2, height: 30, color: AppTheme.border),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(statut, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
                if (commentaire != null && commentaire.isNotEmpty)
                  Text(commentaire, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                Text(dateStr, style: const TextStyle(fontSize: 11, color: AppTheme.textMuted)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
