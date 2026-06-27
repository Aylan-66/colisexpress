import 'dart:convert';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import '../widgets/pagination_bar.dart';

class ValidationScreen extends StatefulWidget {
  const ValidationScreen({super.key});

  @override
  State<ValidationScreen> createState() => _ValidationScreenState();
}

class _ValidationScreenState extends State<ValidationScreen> {
  List<dynamic>? _colis;
  bool _loading = true;
  int _perPage = 20;
  bool _sortDescendant = true;

  List<dynamic> get _colisTrie {
    if (_colis == null) return const [];
    final list = List<dynamic>.from(_colis!);
    list.sort((a, b) {
      final da = DateTime.tryParse((a['dateCreation'] ?? '').toString()) ?? DateTime(1970);
      final db = DateTime.tryParse((b['dateCreation'] ?? '').toString()) ?? DateTime(1970);
      return _sortDescendant ? db.compareTo(da) : da.compareTo(db);
    });
    return list;
  }
  int _currentPage = 1;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final api = context.read<ApiService>();
    final res = await api.getColisEnAttenteValidation();
    if (mounted) setState(() { _colis = res; _loading = false; });
  }

  Future<void> _valider(Map<String, dynamic> c) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Valider la prise en charge ?'),
        content: Text('Confirmez que vous prenez en charge le colis ${c['codeColis']}. Le client pourra alors le déposer au point relais.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Valider'),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    final res = await context.read<ApiService>().validerColis(c['codeColis']);
    if (!mounted) return;
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['error'])));
    } else {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['message'] ?? 'Colis validé')));
      _load();
    }
  }

  Future<void> _refuser(Map<String, dynamic> c) async {
    final motifCtrl = TextEditingController();
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Refuser ce colis'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Le client sera remboursé par l\'administration. Indiquez le motif :',
                  style: TextStyle(fontSize: 13, color: AppTheme.textMuted)),
              const SizedBox(height: 12),
              TextField(
                controller: motifCtrl,
                minLines: 2,
                maxLines: 4,
                decoration: const InputDecoration(labelText: 'MOTIF', hintText: 'Produit interdit, poids dépassé...'),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Refuser'),
          ),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    final motif = motifCtrl.text.trim();
    if (motif.length < 5) {
      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Motif trop court (5 caractères mini).')));
      return;
    }
    final res = await context.read<ApiService>().refuserColis(c['codeColis'], motif);
    if (!mounted) return;
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['error'])));
    } else {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(res['message'] ?? 'Colis refusé')));
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Colis à valider'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : (_colis == null || _colis!.isEmpty)
              ? const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.check_circle, size: 64, color: AppTheme.success),
                        SizedBox(height: 16),
                        Text('Aucun colis en attente', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                        SizedBox(height: 8),
                        Text('Quand un client paie une commande, elle apparaît ici pour validation.',
                            textAlign: TextAlign.center, style: TextStyle(color: AppTheme.textMuted)),
                      ],
                    ),
                  ),
                )
              : Column(
                  children: [
                    Row(
                      children: [
                        Expanded(
                          child: PerPageSelector(
                            value: _perPage,
                            totalItems: _colisTrie.length,
                            onChanged: (n) => setState(() { _perPage = n; _currentPage = 1; }),
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.only(right: 12),
                          child: ActionChip(
                            avatar: Icon(_sortDescendant ? Icons.arrow_downward : Icons.arrow_upward, size: 16),
                            label: Text(_sortDescendant ? 'Récents' : 'Anciens', style: const TextStyle(fontSize: 12)),
                            onPressed: () => setState(() => _sortDescendant = !_sortDescendant),
                          ),
                        ),
                      ],
                    ),
                    Expanded(
                      child: Builder(builder: (_) {
                        final all = _colisTrie;
                        final totalPages = (all.length / _perPage).ceil().clamp(1, 9999);
                        final page = _currentPage.clamp(1, totalPages);
                        final start = (page - 1) * _perPage;
                        final end = (start + _perPage).clamp(0, all.length);
                        final visibles = all.sublist(start, end);
                        return RefreshIndicator(
                          onRefresh: _load,
                          child: ListView.builder(
                            padding: const EdgeInsets.all(16),
                            itemCount: visibles.length + 1,
                            itemBuilder: (_, i) {
                              if (i >= visibles.length) {
                                return PaginationControls(
                                  currentPage: page,
                                  totalPages: totalPages,
                                  onPageChanged: (p) => setState(() => _currentPage = p),
                                );
                              }
                              return _card(visibles[i] as Map<String, dynamic>);
                            },
                          ),
                        );
                      }),
                    ),
                  ],
                ),
    );
  }

  Widget _card(Map<String, dynamic> c) {
    final L = c['longueurCm'];
    final l = c['largeurCm'];
    final h = c['hauteurCm'];
    final dim = (L != null && l != null && h != null) ? '${L}×${l}×${h} cm' : (c['dimensions']?.toString() ?? '—');
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.accent.withValues(alpha: 0.3), width: 1.5),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: Text(c['codeColis']?.toString() ?? '—',
                    style: const TextStyle(fontFamily: 'monospace', fontWeight: FontWeight.w800, fontSize: 14)),
              ),
              Text('${c['total']} €',
                  style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: AppTheme.primary)),
            ],
          ),
          const SizedBox(height: 4),
          Text(c['trajet']?.toString() ?? '—', style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
          Text('${c['segmentDepart']} → ${c['segmentArrivee']}',
              style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
          const Divider(height: 16),
          _info('Client', '${c['client']} • ${c['clientEmail'] ?? '—'}'),
          _info('Destinataire', '${c['nomDestinataire']} • ${c['telephoneDestinataire']}'),
          _info('Contenu', c['descriptionContenu']?.toString() ?? '—'),
          _info('Poids / dimensions', '${c['poidsDeclare']} kg • $dim'),
          _info('Valeur déclarée', '${c['valeurDeclaree']} €'),
          _info('Paiement', c['modeReglement']?.toString() ?? '—'),
          if (c['photoReservation'] != null) ...[
            const SizedBox(height: 10),
            const Text('Photo fournie par le client', style: TextStyle(fontSize: 11, color: AppTheme.textMuted, fontWeight: FontWeight.w700)),
            const SizedBox(height: 6),
            ClipRRect(
              borderRadius: BorderRadius.circular(8),
              child: GestureDetector(
                onTap: () => _voirPhoto(c['photoReservation'].toString()),
                child: Image.memory(
                  _decodePhoto(c['photoReservation'].toString()),
                  height: 160, width: double.infinity, fit: BoxFit.cover,
                  // Downsample en mémoire : évite de décoder l'image pleine résolution (crash mémoire iOS)
                  cacheWidth: 600,
                  gaplessPlayback: true,
                  errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                ),
              ),
            ),
          ],
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: ElevatedButton.icon(
                  icon: const Icon(Icons.check, size: 18),
                  label: const Text('Valider'),
                  style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
                  onPressed: () => _valider(c),
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: OutlinedButton.icon(
                  icon: const Icon(Icons.block, size: 18, color: AppTheme.danger),
                  label: const Text('Refuser', style: TextStyle(color: AppTheme.danger)),
                  onPressed: () => _refuser(c),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Uint8List _decodePhoto(String dataUri) {
    final idx = dataUri.indexOf(',');
    final b64 = idx >= 0 ? dataUri.substring(idx + 1) : dataUri;
    return base64Decode(b64);
  }

  void _voirPhoto(String dataUri) {
    showDialog(
      context: context,
      builder: (ctx) => Dialog(
        child: InteractiveViewer(
          child: Image.memory(_decodePhoto(dataUri), fit: BoxFit.contain, cacheWidth: 1200),
        ),
      ),
    );
  }

  Widget _info(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 110,
              child: Text(label,
                  style: const TextStyle(fontSize: 11, color: AppTheme.textMuted, fontWeight: FontWeight.w700)),
            ),
            Expanded(child: Text(value, style: const TextStyle(fontSize: 12))),
          ],
        ),
      );
}
