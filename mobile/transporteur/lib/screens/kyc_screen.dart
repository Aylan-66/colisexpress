import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class KycScreen extends StatefulWidget {
  const KycScreen({super.key});

  @override
  State<KycScreen> createState() => _KycScreenState();
}

class _KycScreenState extends State<KycScreen> {
  Map<String, dynamic>? _status;
  bool _loading = true;
  String? _uploadingType;
  String? _success;
  String? _error;

  static const _docTypes = [
    ('PieceIdentite', "Pièce d'identité", Icons.badge),
    ('JustificatifAdresse', 'Justificatif de domicile', Icons.home),
    ('Assurance', 'Assurance', Icons.shield),
    ('Permis', 'Permis de conduire', Icons.drive_eta),
    ('Selfie', 'Selfie avec pièce d\'identité', Icons.face),
  ];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final res = await context.read<ApiService>().getKycStatus();
    setState(() {
      _loading = false;
      if (res.containsKey('error')) {
        _error = res['error'];
      } else {
        _status = res;
      }
    });
  }

  Future<void> _upload(String typeDocument) async {
    final picker = ImagePicker();

    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Wrap(children: [
          ListTile(
            leading: const Icon(Icons.camera_alt),
            title: const Text('Prendre une photo'),
            onTap: () => Navigator.pop(ctx, ImageSource.camera),
          ),
          ListTile(
            leading: const Icon(Icons.photo_library),
            title: const Text('Galerie'),
            onTap: () => Navigator.pop(ctx, ImageSource.gallery),
          ),
        ]),
      ),
    );

    if (source == null) return;

    final file = await picker.pickImage(source: source, maxWidth: 1600, imageQuality: 85);
    if (file == null) return;

    setState(() { _uploadingType = typeDocument; _success = null; _error = null; });
    final res = await context.read<ApiService>().uploadKycDocument(typeDocument, File(file.path));

    if (res.containsKey('error')) {
      setState(() { _uploadingType = null; _error = res['error']; });
    } else {
      setState(() { _uploadingType = null; _success = 'Document soumis !'; });
      // Recharger sans tout masquer
      final status = await context.read<ApiService>().getKycStatus();
      if (mounted) setState(() { _status = status.containsKey('error') ? _status : status; });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Vérification KYC')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  if (_success != null)
                    _alert(_success!, AppTheme.success),
                  if (_error != null)
                    _alert(_error!, AppTheme.danger),

                  // Statut global
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(16),
                      child: Row(
                        children: [
                          Icon(
                            _kycIcon,
                            size: 36,
                            color: _kycColor,
                          ),
                          const SizedBox(width: 14),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text('Statut KYC', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 16)),
                                const SizedBox(height: 4),
                                Text(_kycLabel,
                                    style: TextStyle(color: _kycColor, fontWeight: FontWeight.w600, fontSize: 13)),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 16),
                  const Text('DOCUMENTS REQUIS',
                      style: TextStyle(fontSize: 11, fontWeight: FontWeight.w800, color: AppTheme.textMuted, letterSpacing: 1)),
                  const SizedBox(height: 10),

                  ..._docTypes.map((t) => _docCard(t.$1, t.$2, t.$3)),
                ],
              ),
            ),
    );
  }

  Widget _docCard(String type, String label, IconData icon) {
    final docs = (_status?['documents'] as List?) ?? [];
    final existing = docs.cast<Map<String, dynamic>>().where((d) => d['typeDocument'] == type).firstOrNull;
    final statut = existing?['statut']?.toString();
    final isUploading = _uploadingType == type;
    final isOtherUploading = _uploadingType != null && !isUploading;

    Color badgeColor = AppTheme.textMuted;
    String badgeText = 'Non soumis';
    if (isUploading) { badgeColor = AppTheme.primary; badgeText = 'Envoi...'; }
    else if (statut == 'EnAttente') { badgeColor = AppTheme.accent; badgeText = 'En attente'; }
    else if (statut == 'Valide') { badgeColor = AppTheme.success; badgeText = 'Validé'; }
    else if (statut == 'Rejete') { badgeColor = AppTheme.danger; badgeText = 'Rejeté'; }

    return Opacity(
      opacity: isOtherUploading ? 0.5 : 1.0,
      child: Card(
        margin: const EdgeInsets.only(bottom: 10),
        child: ListTile(
          contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
          leading: Container(
            width: 44, height: 44,
            decoration: BoxDecoration(
              color: AppTheme.primary.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(10),
            ),
            child: isUploading
                ? const Padding(padding: EdgeInsets.all(12), child: CircularProgressIndicator(strokeWidth: 2))
                : Icon(icon, color: AppTheme.primary, size: 22),
          ),
          title: Text(label, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 14)),
          subtitle: existing != null
              ? Text(existing['nomFichier'] ?? '', style: const TextStyle(fontSize: 11, color: AppTheme.textMuted))
              : null,
          trailing: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: badgeColor.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(6),
                ),
                child: Text(badgeText,
                    style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: badgeColor)),
              ),
              const SizedBox(width: 8),
              IconButton(
                icon: Icon(existing == null ? Icons.add_a_photo : Icons.refresh, size: 20),
                color: AppTheme.primary,
                onPressed: isUploading || isOtherUploading ? null : () => _upload(type),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _alert(String msg, Color color) => Container(
        width: double.infinity,
        padding: const EdgeInsets.all(12),
        margin: const EdgeInsets.only(bottom: 12),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(8),
        ),
        child: Text(msg, style: TextStyle(color: color, fontWeight: FontWeight.w600, fontSize: 13)),
      );

  String get _kycStatut => _status?['statutKyc']?.toString() ?? 'NonSoumis';
  Color get _kycColor => switch (_kycStatut) {
        'Valide' => AppTheme.success,
        'EnAttente' => AppTheme.accent,
        'Rejete' => AppTheme.danger,
        _ => AppTheme.textMuted,
      };
  IconData get _kycIcon => switch (_kycStatut) {
        'Valide' => Icons.verified,
        'EnAttente' => Icons.hourglass_top,
        'Rejete' => Icons.cancel,
        _ => Icons.upload_file,
      };
  String get _kycLabel => switch (_kycStatut) {
        'Valide' => 'Vérifié — vous pouvez publier des trajets',
        'EnAttente' => 'En attente de validation par l\'admin',
        'Rejete' => 'Rejeté — veuillez re-soumettre vos documents',
        _ => 'Non soumis — uploadez vos documents ci-dessous',
      };
}
