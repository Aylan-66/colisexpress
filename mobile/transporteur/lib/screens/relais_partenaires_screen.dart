import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import '../widgets/pagination_bar.dart';

class RelaisPartenairesScreen extends StatefulWidget {
  const RelaisPartenairesScreen({super.key});

  @override
  State<RelaisPartenairesScreen> createState() => _RelaisPartenairesScreenState();
}

class _RelaisPartenairesScreenState extends State<RelaisPartenairesScreen> {
  List<dynamic>? _relais;
  bool _loading = true;
  int _perPage = 20;
  int _currentPage = 1;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final api = context.read<ApiService>();
    final res = await api.getRelaisPartenaires();
    if (mounted) setState(() { _relais = res; _loading = false; });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Mes points relais'),
        actions: [IconButton(icon: const Icon(Icons.refresh), onPressed: _load)],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : (_relais == null || _relais!.isEmpty)
              ? const Center(
                  child: Padding(
                    padding: EdgeInsets.all(32),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Icon(Icons.store_outlined, size: 64, color: AppTheme.textMuted),
                        SizedBox(height: 16),
                        Text('Aucun point relais', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                        SizedBox(height: 8),
                        Text('Les points relais associés à vos trajets actifs apparaîtront ici.',
                            textAlign: TextAlign.center, style: TextStyle(color: AppTheme.textMuted)),
                      ],
                    ),
                  ),
                )
              : Column(
                  children: [
                    PerPageSelector(
                      value: _perPage,
                      totalItems: _relais!.length,
                      onChanged: (n) => setState(() { _perPage = n; _currentPage = 1; }),
                    ),
                    Expanded(
                      child: Builder(builder: (_) {
                        final all = _relais!;
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

  Widget _card(Map<String, dynamic> r) {
    final colis = (r['colisEnAttente'] as List?) ?? [];
    return Container(
      margin: const EdgeInsets.only(bottom: 12),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: AppTheme.surface,
        border: Border.all(color: AppTheme.border),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.store, color: AppTheme.primary),
              const SizedBox(width: 8),
              Expanded(
                child: Text(r['nom']?.toString() ?? '—',
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800)),
              ),
              if (colis.isNotEmpty)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                  decoration: BoxDecoration(
                    color: AppTheme.accent,
                    borderRadius: BorderRadius.circular(12),
                  ),
                  child: Text('${colis.length} colis',
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 11)),
                ),
            ],
          ),
          const SizedBox(height: 4),
          Text('${r['adresse']}, ${r['ville']} (${r['pays']})',
              style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
          Text('📞 ${r['telephone']}', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
          if (colis.isNotEmpty) ...[
            const Divider(height: 20),
            const Text('Colis en attente de récupération destinataire',
                style: TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppTheme.textMuted)),
            const SizedBox(height: 6),
            ...colis.map((c) {
              final cm = c as Map<String, dynamic>;
              return Padding(
                padding: const EdgeInsets.only(bottom: 4),
                child: Row(
                  children: [
                    Text(cm['codeColis']?.toString() ?? '—',
                        style: const TextStyle(fontFamily: 'monospace', fontSize: 12, fontWeight: FontWeight.w700)),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text('${cm['nomDestinataire']} • ${cm['trajet']}',
                          style: const TextStyle(fontSize: 12, color: AppTheme.textMuted),
                          overflow: TextOverflow.ellipsis),
                    ),
                  ],
                ),
              );
            }),
          ],
        ],
      ),
    );
  }
}
