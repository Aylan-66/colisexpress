import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'tarif_edit_screen.dart';

class TarifsScreen extends StatefulWidget {
  const TarifsScreen({super.key});

  @override
  State<TarifsScreen> createState() => _TarifsScreenState();
}

class _TarifsScreenState extends State<TarifsScreen> {
  List<dynamic>? _tarifs;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final api = context.read<ApiService>();
    final res = await api.getMesTarifs();
    if (mounted) setState(() { _tarifs = res; _loading = false; });
  }

  Future<void> _create() async {
    await Navigator.push(context, MaterialPageRoute(builder: (_) => const TarifEditScreen()));
    _load();
  }

  Future<void> _edit(Map<String, dynamic> t) async {
    await Navigator.push(context, MaterialPageRoute(builder: (_) => TarifEditScreen(tarif: t)));
    _load();
  }

  Future<void> _supprimer(Map<String, dynamic> t) async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Désactiver ce tarif ?'),
        content: const Text('Le tarif sera désactivé mais reste consultable. Les trajets utilisant ce tarif ne sont pas affectés.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('Désactiver')),
        ],
      ),
    );
    if (ok != true || !mounted) return;
    await context.read<ApiService>().deleteTarif(t['id']);
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mes tarifs'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: _create,
        icon: const Icon(Icons.add),
        label: const Text('Nouveau tarif'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : (_tarifs == null || _tarifs!.isEmpty)
              ? _empty()
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _tarifs!.length,
                    itemBuilder: (_, i) => _card(_tarifs![i] as Map<String, dynamic>),
                  ),
                ),
    );
  }

  Widget _empty() => Center(
        child: Padding(
          padding: const EdgeInsets.all(32),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const Icon(Icons.euro, size: 64, color: AppTheme.textMuted),
              const SizedBox(height: 16),
              const Text('Aucun tarif configuré', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
              const SizedBox(height: 8),
              const Text('Créez des templates de tarifs réutilisables lors de la création de vos trajets.',
                  textAlign: TextAlign.center, style: TextStyle(color: AppTheme.textMuted)),
              const SizedBox(height: 24),
              ElevatedButton.icon(onPressed: _create, icon: const Icon(Icons.add), label: const Text('Créer un tarif')),
            ],
          ),
        ),
      );

  Widget _card(Map<String, dynamic> t) {
    final estActif = t['estActif'] == true;
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.border),
        borderRadius: BorderRadius.circular(12),
      ),
      child: InkWell(
        onTap: () => _edit(t),
        borderRadius: BorderRadius.circular(12),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(t['nom']?.toString() ?? '—',
                        style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                  ),
                  if (!estActif)
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: AppTheme.textMuted.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: const Text('Désactivé', style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700)),
                    ),
                  IconButton(icon: const Icon(Icons.delete_outline, size: 20), onPressed: () => _supprimer(t)),
                ],
              ),
              if (t['description'] != null && (t['description'] as String).isNotEmpty)
                Padding(
                  padding: const EdgeInsets.only(top: 4, bottom: 8),
                  child: Text(t['description'].toString(),
                      style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                ),
              _periodeBadge(t),
              const Divider(height: 16),
              _line('Standard', '${t['prixAuKiloStandard']} €/kg jusqu\'à ${t['seuilStandardKg']} kg'),
              _line('Lourd', 'forfait ${t['forfaitLourd']} € + ${t['prixAuKiloLourd']} €/kg au-delà'),
              _line('Hors gabarit',
                  'forfait ${t['forfaitHorsGabarit']} € + ${t['prixAuKiloHorsGabarit']} €/kg si dimension > ${t['longueurMaxStandardCm']}×${t['largeurMaxStandardCm']}×${t['hauteurMaxStandardCm']} cm'),
            ],
          ),
        ),
      ),
    );
  }

  Widget _periodeBadge(Map<String, dynamic> t) {
    final dd = DateTime.tryParse(t['dateDebut']?.toString() ?? '');
    final df = DateTime.tryParse(t['dateFin']?.toString() ?? '');
    if (dd == null && df == null) return const SizedBox.shrink();
    String fmt(DateTime d) => '${d.day.toString().padLeft(2, '0')}/${d.month.toString().padLeft(2, '0')}/${d.year}';
    final txt = dd != null && df != null
        ? 'Du ${fmt(dd)} au ${fmt(df)}'
        : dd != null
            ? 'À partir du ${fmt(dd)}'
            : 'Jusqu\'au ${fmt(df!)}';
    final now = DateTime.now();
    final actif = (dd == null || !now.isBefore(dd)) && (df == null || !now.isAfter(df));
    final color = actif ? AppTheme.accent : AppTheme.textMuted;
    return Padding(
      padding: const EdgeInsets.only(top: 6),
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.12),
          borderRadius: BorderRadius.circular(6),
        ),
        child: Row(mainAxisSize: MainAxisSize.min, children: [
          Icon(Icons.calendar_today, size: 12, color: color),
          const SizedBox(width: 4),
          Text(txt, style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: color)),
        ]),
      ),
    );
  }

  Widget _line(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 4),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            SizedBox(
              width: 100,
              child: Text(label,
                  style: const TextStyle(fontSize: 12, color: AppTheme.textMuted, fontWeight: FontWeight.w700)),
            ),
            Expanded(child: Text(value, style: const TextStyle(fontSize: 12))),
          ],
        ),
      );
}
