import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'colis_detail_screen.dart';

class ColisScreen extends StatefulWidget {
  const ColisScreen({super.key});

  @override
  State<ColisScreen> createState() => _ColisScreenState();
}

class _ColisScreenState extends State<ColisScreen> {
  final _codeCtrl = TextEditingController();
  List<dynamic> _commandes = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    _commandes = await context.read<ApiService>().getMesCommandes();
    setState(() => _loading = false);
  }

  void _searchByCode() {
    final code = _codeCtrl.text.trim();
    if (code.isEmpty) return;
    Navigator.push(context,
        MaterialPageRoute(builder: (_) => ColisDetailScreen(codeColis: code)));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Colis')),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _codeCtrl,
                    decoration: const InputDecoration(
                      labelText: 'CODE COLIS',
                      hintText: 'COL-2026-0001',
                      prefixIcon: Icon(Icons.search, size: 20),
                    ),
                    textInputAction: TextInputAction.search,
                    onSubmitted: (_) => _searchByCode(),
                  ),
                ),
                const SizedBox(width: 8),
                ElevatedButton(
                  onPressed: _searchByCode,
                  child: const Text('Voir'),
                ),
              ],
            ),
          ),
          Expanded(
            child: _loading
                ? const Center(child: CircularProgressIndicator())
                : _commandes.isEmpty
                    ? const Center(
                        child: Text('Aucun colis en cours',
                            style: TextStyle(color: AppTheme.textMuted)),
                      )
                    : RefreshIndicator(
                        onRefresh: _load,
                        child: ListView.builder(
                          padding: const EdgeInsets.symmetric(horizontal: 16),
                          itemCount: _commandes.length,
                          itemBuilder: (ctx, i) {
                            final c = _commandes[i];
                            return _ColisListItem(
                              commande: c,
                              onTap: () {
                                final code = c['codeColis'] ?? '';
                                if (code.isNotEmpty) {
                                  Navigator.push(context,
                                      MaterialPageRoute(builder: (_) => ColisDetailScreen(codeColis: code)));
                                }
                              },
                            );
                          },
                        ),
                      ),
          ),
        ],
      ),
    );
  }
}

class _ColisListItem extends StatelessWidget {
  final Map<String, dynamic> commande;
  final VoidCallback onTap;

  const _ColisListItem({required this.commande, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final statut = commande['statutColis']?.toString() ?? '—';
    final code = commande['codeColis'] ?? '—';
    final trajet = commande['trajet'] ?? '—';

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        onTap: onTap,
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        leading: Container(
          width: 44, height: 44,
          decoration: BoxDecoration(
            color: AppTheme.primary.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(10),
          ),
          child: const Icon(Icons.inventory_2, color: AppTheme.primary, size: 22),
        ),
        title: Text(code,
            style: const TextStyle(fontWeight: FontWeight.w700, fontFamily: 'monospace', fontSize: 14)),
        subtitle: Text(trajet, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: AppTheme.primary.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(6),
          ),
          child: Text(statut,
              style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: AppTheme.primary)),
        ),
      ),
    );
  }
}
