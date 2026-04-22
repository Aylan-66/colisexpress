import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class RegisterScreen extends StatefulWidget {
  final void Function({bool isNew}) onRegister;
  const RegisterScreen({super.key, required this.onRegister});

  @override
  State<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends State<RegisterScreen> {
  final _prenomCtrl = TextEditingController();
  final _nomCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _telCtrl = TextEditingController();
  final _pwdCtrl = TextEditingController();
  final _pwd2Ctrl = TextEditingController();
  final _nomRelaisCtrl = TextEditingController();
  final _adresseCtrl = TextEditingController();
  final _villeCtrl = TextEditingController();
  String _pays = 'France';
  bool _loading = false;
  String? _error;

  static const _paysOptions = ['France', 'Algérie', 'Maroc', 'Tunisie'];

  Future<void> _register() async {
    if (_pwdCtrl.text != _pwd2Ctrl.text) {
      setState(() => _error = 'Les mots de passe ne correspondent pas.');
      return;
    }
    if (_nomRelaisCtrl.text.trim().isEmpty) {
      setState(() => _error = 'Le nom du relais est obligatoire.');
      return;
    }
    setState(() {
      _loading = true;
      _error = null;
    });

    final api = context.read<ApiService>();
    final res = await api.register(
      prenom: _prenomCtrl.text.trim(),
      nom: _nomCtrl.text.trim(),
      email: _emailCtrl.text.trim(),
      telephone: _telCtrl.text.trim(),
      motDePasse: _pwdCtrl.text,
      nomRelais: _nomRelaisCtrl.text.trim(),
      adresse: _adresseCtrl.text.trim(),
      ville: _villeCtrl.text.trim(),
      pays: _pays,
    );

    setState(() => _loading = false);
    if (res.containsKey('error')) {
      setState(() => _error = res['error']);
    } else {
      widget.onRegister(isNew: true);
      if (mounted) Navigator.of(context).popUntil((route) => route.isFirst);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Inscription Point Relais')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (_error != null)
              Container(
                width: double.infinity,
                padding: const EdgeInsets.all(12),
                margin: const EdgeInsets.only(bottom: 16),
                decoration: BoxDecoration(
                  color: AppTheme.danger.withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(_error!,
                    style: const TextStyle(
                        color: AppTheme.danger, fontSize: 13)),
              ),

            // Section Identite
            const Text('IDENTITE',
                style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                    color: AppTheme.textMuted,
                    letterSpacing: 1)),
            const SizedBox(height: 10),
            Row(children: [
              Expanded(
                  child: TextField(
                      controller: _prenomCtrl,
                      decoration:
                          const InputDecoration(labelText: 'PRENOM'))),
              const SizedBox(width: 10),
              Expanded(
                  child: TextField(
                      controller: _nomCtrl,
                      decoration:
                          const InputDecoration(labelText: 'NOM'))),
            ]),
            const SizedBox(height: 10),
            TextField(
                controller: _emailCtrl,
                decoration: const InputDecoration(labelText: 'EMAIL'),
                keyboardType: TextInputType.emailAddress),
            const SizedBox(height: 10),
            TextField(
                controller: _telCtrl,
                decoration:
                    const InputDecoration(labelText: 'TELEPHONE'),
                keyboardType: TextInputType.phone),
            const SizedBox(height: 10),
            TextField(
                controller: _pwdCtrl,
                decoration:
                    const InputDecoration(labelText: 'MOT DE PASSE'),
                obscureText: true),
            const SizedBox(height: 10),
            TextField(
                controller: _pwd2Ctrl,
                decoration: const InputDecoration(
                    labelText: 'CONFIRMER MOT DE PASSE'),
                obscureText: true),

            // Section Point Relais
            const SizedBox(height: 24),
            const Text('POINT RELAIS',
                style: TextStyle(
                    fontSize: 11,
                    fontWeight: FontWeight.w800,
                    color: AppTheme.textMuted,
                    letterSpacing: 1)),
            const SizedBox(height: 10),
            TextField(
                controller: _nomRelaisCtrl,
                decoration:
                    const InputDecoration(labelText: 'NOM DU RELAIS')),
            const SizedBox(height: 10),
            TextField(
                controller: _adresseCtrl,
                decoration:
                    const InputDecoration(labelText: 'ADRESSE')),
            const SizedBox(height: 10),
            TextField(
                controller: _villeCtrl,
                decoration:
                    const InputDecoration(labelText: 'VILLE')),
            const SizedBox(height: 10),
            DropdownButtonFormField<String>(
              initialValue: _pays,
              decoration: const InputDecoration(labelText: 'PAYS'),
              items: _paysOptions
                  .map((p) =>
                      DropdownMenuItem(value: p, child: Text(p)))
                  .toList(),
              onChanged: (v) => setState(() => _pays = v!),
            ),

            const SizedBox(height: 28),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _register,
                child: _loading
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white))
                    : const Text('Créer mon compte'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
