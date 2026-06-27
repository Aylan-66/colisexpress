import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class MesPointsScreen extends StatefulWidget {
  const MesPointsScreen({super.key});

  @override
  State<MesPointsScreen> createState() => _MesPointsScreenState();
}

class _MesPointsScreenState extends State<MesPointsScreen> {
  List<dynamic> _points = [];
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    _points = await context.read<ApiService>().getMesPoints();
    if (mounted) setState(() => _loading = false);
  }

  Future<void> _editer({Map<String, dynamic>? existant}) async {
    final saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(builder: (_) => _PointEditScreen(point: existant)),
    );
    if (saved == true) _load();
  }

  Future<void> _supprimer(Map<String, dynamic> p) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Supprimer ce point ?'),
        content: Text('"${p['nom']}" sera retiré de votre liste.'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.danger),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Supprimer'),
          ),
        ],
      ),
    );
    if (confirm != true) return;
    await context.read<ApiService>().deletePoint(p['id']);
    if (!mounted) return;
    _load();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Mes points perso')),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _editer(),
        icon: const Icon(Icons.add),
        label: const Text('Ajouter'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _points.isEmpty
              ? const Center(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: Text(
                      'Aucun point pour le moment.\n\nAjoutez vos lieux de dépôt / récupération personnels (garage, atelier, parking…) pour les réutiliser dans vos trajets.',
                      textAlign: TextAlign.center,
                      style: TextStyle(color: Color(0xFF78716C)),
                    ),
                  ),
                )
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView.builder(
                    padding: const EdgeInsets.all(16),
                    itemCount: _points.length,
                    itemBuilder: (ctx, i) {
                      final p = _points[i] as Map<String, dynamic>;
                      return Card(
                        margin: const EdgeInsets.only(bottom: 10),
                        child: ListTile(
                          leading: const CircleAvatar(child: Icon(Icons.place)),
                          title: Text(p['nom']?.toString() ?? '—',
                              style: const TextStyle(fontWeight: FontWeight.w700)),
                          subtitle: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('${p['adresse'] ?? ''}'),
                              Text('${p['codePostal'] ?? ''} ${p['ville'] ?? ''} — ${p['pays'] ?? ''}',
                                  style: const TextStyle(fontSize: 12)),
                              if ((p['telephone'] ?? '').toString().isNotEmpty)
                                Text('☎ ${p['telephone']}', style: const TextStyle(fontSize: 12)),
                              if ((p['horaires'] ?? '').toString().isNotEmpty)
                                Text('🕒 ${p['horaires']}', style: const TextStyle(fontSize: 12)),
                              if ((p['instructions'] ?? '').toString().isNotEmpty)
                                Padding(
                                  padding: const EdgeInsets.only(top: 4),
                                  child: Text('ℹ ${p['instructions']}',
                                      style: const TextStyle(fontSize: 11, fontStyle: FontStyle.italic)),
                                ),
                            ],
                          ),
                          isThreeLine: true,
                          trailing: PopupMenuButton<String>(
                            onSelected: (v) {
                              if (v == 'edit') _editer(existant: p);
                              if (v == 'del') _supprimer(p);
                            },
                            itemBuilder: (_) => const [
                              PopupMenuItem(value: 'edit', child: Text('Modifier')),
                              PopupMenuItem(value: 'del', child: Text('Supprimer')),
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ),
    );
  }
}

class _PointEditScreen extends StatefulWidget {
  final Map<String, dynamic>? point;
  const _PointEditScreen({this.point});

  @override
  State<_PointEditScreen> createState() => _PointEditScreenState();
}

class _PointEditScreenState extends State<_PointEditScreen> {
  final _formKey = GlobalKey<FormState>();
  final _nom = TextEditingController();
  final _adresse = TextEditingController();
  final _codePostal = TextEditingController();
  final _ville = TextEditingController();
  final _pays = TextEditingController(text: 'France');
  final _telephone = TextEditingController();
  final _horaires = TextEditingController();
  final _instructions = TextEditingController();
  bool _saving = false;

  @override
  void initState() {
    super.initState();
    final p = widget.point;
    if (p != null) {
      _nom.text = p['nom']?.toString() ?? '';
      _adresse.text = p['adresse']?.toString() ?? '';
      _codePostal.text = p['codePostal']?.toString() ?? '';
      _ville.text = p['ville']?.toString() ?? '';
      _pays.text = p['pays']?.toString() ?? 'France';
      _telephone.text = p['telephone']?.toString() ?? '';
      _horaires.text = p['horaires']?.toString() ?? '';
      _instructions.text = p['instructions']?.toString() ?? '';
    }
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() => _saving = true);
    final api = context.read<ApiService>();
    final data = {
      'nom': _nom.text.trim(),
      'adresse': _adresse.text.trim(),
      'codePostal': _codePostal.text.trim(),
      'ville': _ville.text.trim(),
      'pays': _pays.text.trim(),
      'telephone': _telephone.text.trim(),
      'horaires': _horaires.text.trim(),
      'instructions': _instructions.text.trim(),
    };
    final res = widget.point == null
        ? await api.createPoint(data)
        : await api.updatePoint(widget.point!['id'], data);
    if (!mounted) return;
    setState(() => _saving = false);
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(res['error']), backgroundColor: AppTheme.danger));
    } else {
      Navigator.pop(context, true);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.point == null ? 'Nouveau point' : 'Modifier le point')),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            TextFormField(
              controller: _nom,
              decoration: const InputDecoration(labelText: 'Nom du point *', hintText: 'Ex: Mon garage Lyon'),
              validator: (v) => v == null || v.trim().isEmpty ? 'Nom requis' : null,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _adresse,
              decoration: const InputDecoration(labelText: 'Adresse *', hintText: '12 rue de la Paix'),
              validator: (v) => v == null || v.trim().isEmpty ? 'Adresse requise' : null,
            ),
            const SizedBox(height: 12),
            Row(children: [
              SizedBox(
                width: 120,
                child: TextFormField(
                  controller: _codePostal,
                  decoration: const InputDecoration(labelText: 'CP'),
                  keyboardType: TextInputType.number,
                ),
              ),
              const SizedBox(width: 8),
              Expanded(
                child: TextFormField(
                  controller: _ville,
                  decoration: const InputDecoration(labelText: 'Ville *'),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Ville requise' : null,
                ),
              ),
            ]),
            const SizedBox(height: 12),
            TextFormField(
              controller: _pays,
              decoration: const InputDecoration(labelText: 'Pays'),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _telephone,
              decoration: const InputDecoration(labelText: 'Téléphone (optionnel)'),
              keyboardType: TextInputType.phone,
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _horaires,
              decoration: const InputDecoration(
                labelText: 'Horaires (optionnel)',
                hintText: 'Ex: Lun-Ven 9h-18h, Sam 10h-13h',
              ),
            ),
            const SizedBox(height: 12),
            TextFormField(
              controller: _instructions,
              decoration: const InputDecoration(
                labelText: 'Instructions d\'accès (optionnel)',
                hintText: 'Digicode, étage, bât, signalement…',
              ),
              maxLines: 3,
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _saving ? null : _save,
              child: _saving
                  ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2))
                  : const Text('Enregistrer'),
            ),
          ],
        ),
      ),
    );
  }
}
