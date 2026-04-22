import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class ColisScreen extends StatefulWidget {
  const ColisScreen({super.key});

  @override
  State<ColisScreen> createState() => _ColisScreenState();
}

class _ColisScreenState extends State<ColisScreen> {
  List<dynamic> _colisList = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    _colisList = await context.read<ApiService>().getColisList();
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _confirmerDepot(String codeColis) async {
    final api = context.read<ApiService>();
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirmer le depot'),
        content: Text(
            'Confirmez-vous la reception du colis $codeColis dans votre point relais ?'),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx, false),
              child: const Text('Annuler')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Confirmer')),
        ],
      ),
    );
    if (confirm != true) return;

    final res = await api.confirmerDepot(codeColis);
    if (!mounted) return;

    if (res.containsKey('error')) {
      _showSnackbar(res['error'], AppTheme.danger);
    } else {
      _showSnackbar('Depot confirme pour $codeColis', AppTheme.success);
      _load();
    }
  }

  Future<void> _confirmerRetrait(String codeColis) async {
    final api = context.read<ApiService>();
    final codeCtrl = TextEditingController();
    final code = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Confirmer le retrait'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Colis : $codeColis',
                style: const TextStyle(fontWeight: FontWeight.w600)),
            const SizedBox(height: 12),
            TextField(
              controller: codeCtrl,
              decoration: const InputDecoration(
                labelText: 'CODE DE RETRAIT (4 chiffres)',
                hintText: '0000',
              ),
              keyboardType: TextInputType.number,
              maxLength: 4,
              autofocus: true,
            ),
          ],
        ),
        actions: [
          TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('Annuler')),
          ElevatedButton(
              onPressed: () => Navigator.pop(ctx, codeCtrl.text.trim()),
              child: const Text('Valider le retrait')),
        ],
      ),
    );

    if (code == null || code.isEmpty) return;

    final res = await api.confirmerRetrait(codeColis, code);
    if (!mounted) return;

    if (res.containsKey('error')) {
      _showSnackbar(res['error'], AppTheme.danger);
    } else {
      _showSnackbar(
          'Retrait confirme pour $codeColis', AppTheme.success);
      _load();
    }
  }

  void _showSnackbar(String msg, Color color) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Text(msg, style: const TextStyle(fontWeight: FontWeight.w600)),
        backgroundColor: color,
        behavior: SnackBarBehavior.floating,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(8)),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Colis au relais')),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _colisList.isEmpty
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.inventory_2_outlined,
                          size: 64, color: AppTheme.textMuted.withValues(alpha: 0.4)),
                      const SizedBox(height: 16),
                      const Text('Aucun colis pour le moment',
                          style: TextStyle(
                              color: AppTheme.textMuted,
                              fontSize: 15,
                              fontWeight: FontWeight.w600)),
                      const SizedBox(height: 8),
                      const Text(
                          'Les colis assignes a votre relais apparaitront ici.',
                          style: TextStyle(
                              color: AppTheme.textMuted, fontSize: 13)),
                    ],
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _colisList.length,
                    itemBuilder: (ctx, i) {
                      final colis = _colisList[i] as Map<String, dynamic>;
                      return _ColisCard(
                        colis: colis,
                        onConfirmerDepot: () => _confirmerDepot(
                            colis['codeColis']?.toString() ?? ''),
                        onConfirmerRetrait: () => _confirmerRetrait(
                            colis['codeColis']?.toString() ?? ''),
                      );
                    },
                  ),
                ),
    );
  }
}

class _ColisCard extends StatelessWidget {
  final Map<String, dynamic> colis;
  final VoidCallback onConfirmerDepot;
  final VoidCallback onConfirmerRetrait;

  const _ColisCard({
    required this.colis,
    required this.onConfirmerDepot,
    required this.onConfirmerRetrait,
  });

  @override
  Widget build(BuildContext context) {
    final code = colis['codeColis']?.toString() ?? '-';
    final statut = colis['statut']?.toString() ?? colis['statutColis']?.toString() ?? '-';
    final destinataire = colis['nomDestinataire']?.toString() ?? '-';
    final trajet = colis['trajet']?.toString() ?? '';

    final (Color statusColor, String statusLabel) = _statusInfo(statut);

    final bool showDepot = statut == 'EnAttenteDepot' ||
        statut == 'DeposeParClient' ||
        statut == 'ArriveDansPaysDest';
    final bool showRetrait = statut == 'ReceptionneParPointRelais' ||
        statut == 'DisponibleAuRetrait';

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Header
            Row(
              children: [
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: AppTheme.primary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.inventory_2,
                      color: AppTheme.primary, size: 22),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(code,
                          style: const TextStyle(
                              fontWeight: FontWeight.w700,
                              fontFamily: 'monospace',
                              fontSize: 15)),
                      if (trajet.isNotEmpty)
                        Text(trajet,
                            style: const TextStyle(
                                fontSize: 12,
                                color: AppTheme.textMuted)),
                    ],
                  ),
                ),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: statusColor.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(statusLabel,
                      style: TextStyle(
                          fontSize: 10,
                          fontWeight: FontWeight.w700,
                          color: statusColor)),
                ),
              ],
            ),

            const Divider(height: 24),

            // Destinataire
            Row(
              children: [
                const Icon(Icons.person_outline,
                    size: 16, color: AppTheme.textMuted),
                const SizedBox(width: 6),
                Text('Destinataire : $destinataire',
                    style: const TextStyle(
                        fontSize: 13, color: AppTheme.textMuted)),
              ],
            ),

            // Actions
            if (showDepot || showRetrait) ...[
              const SizedBox(height: 14),
              Row(
                children: [
                  if (showDepot)
                    Expanded(
                      child: ElevatedButton.icon(
                        icon: const Icon(Icons.archive, size: 16),
                        label: const Text('Confirmer reception',
                            style: TextStyle(fontSize: 12)),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.primary,
                          padding: const EdgeInsets.symmetric(vertical: 10),
                        ),
                        onPressed: onConfirmerDepot,
                      ),
                    ),
                  if (showDepot && showRetrait) const SizedBox(width: 8),
                  if (showRetrait)
                    Expanded(
                      child: ElevatedButton.icon(
                        icon: const Icon(Icons.how_to_reg, size: 16),
                        label: const Text('Confirmer retrait',
                            style: TextStyle(fontSize: 12)),
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppTheme.success,
                          padding: const EdgeInsets.symmetric(vertical: 10),
                        ),
                        onPressed: onConfirmerRetrait,
                      ),
                    ),
                ],
              ),
            ],
          ],
        ),
      ),
    );
  }

  (Color, String) _statusInfo(String statut) {
    return switch (statut) {
      'EnAttenteDepot' => (AppTheme.accent, 'En attente depot'),
      'DeposeParClient' => (AppTheme.accent, 'Depose par client'),
      'ArriveDansPaysDest' => (AppTheme.primary, 'Arrive destination'),
      'ReceptionneParPointRelais' => (AppTheme.primary, 'Receptionne'),
      'DisponibleAuRetrait' => (AppTheme.success, 'Disponible au retrait'),
      'RetireParDestinataire' => (AppTheme.success, 'Retire'),
      'LivraisonCloturee' => (AppTheme.success, 'Cloture'),
      'Incident' => (AppTheme.danger, 'Incident'),
      'Refuse' => (AppTheme.danger, 'Refuse'),
      'Endommage' => (AppTheme.danger, 'Endommage'),
      'Perdu' => (AppTheme.danger, 'Perdu'),
      _ => (AppTheme.textMuted, statut),
    };
  }
}
