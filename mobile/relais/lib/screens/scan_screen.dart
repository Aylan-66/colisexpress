import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

enum ScanMode { depot, retrait }

class ScanScreen extends StatefulWidget {
  const ScanScreen({super.key});

  @override
  State<ScanScreen> createState() => _ScanScreenState();
}

class _ScanScreenState extends State<ScanScreen> {
  final MobileScannerController _controller = MobileScannerController();
  ScanMode? _mode;
  bool _processing = false;
  Map<String, dynamic>? _result;
  String? _error;
  String? _scannedCode;

  void _selectMode(ScanMode m) {
    setState(() {
      _mode = m;
      _result = null;
      _error = null;
      _scannedCode = null;
    });
    _controller.start();
  }

  void _onDetect(BarcodeCapture capture) {
    if (_processing || _mode == null) return;
    final barcode = capture.barcodes.firstOrNull;
    if (barcode == null || barcode.rawValue == null) return;

    final code = barcode.rawValue!;
    if (!code.startsWith('COL-')) return;

    setState(() { _processing = true; _scannedCode = code; _error = null; _result = null; });
    _controller.stop();
    _processScan(code);
  }

  Future<void> _processScan(String code) async {
    final api = context.read<ApiService>();
    final modeStr = _mode == ScanMode.retrait ? 'retrait' : 'depot';
    final res = await api.scanColis(code, mode: modeStr);

    if (!mounted) return;
    setState(() => _processing = false);

    if (res.containsKey('error')) {
      final action = res['action']?.toString();
      if (action == 'paiement_requis') {
        _showPaiementDialog(res);
      } else {
        setState(() => _error = res['error']);
      }
    } else {
      final action = res['action']?.toString();
      if (action == 'retrait_requis') {
        _showRetraitDialog(code);
      } else {
        setState(() => _result = res);
      }
    }
  }

  Future<void> _showPaiementDialog(Map<String, dynamic> res) async {
    final commandeId = res['commandeId']?.toString();
    final montant = res['montant']?.toString() ?? '0';

    final confirmed = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: const Text('Paiement espèces requis'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('Montant à encaisser : $montant €',
                style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
            const SizedBox(height: 12),
            const Text('Confirmez que vous avez reçu le paiement en espèces avant de scanner le colis. Ce montant sera ajouté à votre solde dû à la plateforme.',
                style: TextStyle(fontSize: 13, color: AppTheme.textMuted)),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: const Text('Annuler'),
          ),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
            child: const Text('Paiement reçu'),
          ),
        ],
      ),
    );

    if (confirmed == true && commandeId != null && mounted) {
      final api = context.read<ApiService>();
      await api.validerPaiementEspeces(commandeId);
      _processScan(_scannedCode!);
    }
  }

  Future<void> _showRetraitDialog(String code) async {
    final codeCtrl = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: const Text('Code de retrait'),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Text('Demandez le code 4 chiffres au destinataire.', style: TextStyle(fontSize: 13, color: AppTheme.textMuted)),
            const SizedBox(height: 16),
            TextField(
              controller: codeCtrl,
              decoration: const InputDecoration(labelText: 'CODE DE RETRAIT', hintText: '1234'),
              keyboardType: TextInputType.number,
              maxLength: 4,
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 24, fontWeight: FontWeight.w800, letterSpacing: 8),
            ),
          ],
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
            child: const Text('Valider le retrait'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;

    final api = context.read<ApiService>();
    final res = await api.confirmerRetrait(code, codeCtrl.text.trim());
    if (!mounted) return;

    if (res.containsKey('error')) {
      setState(() => _error = res['error']);
    } else {
      setState(() => _result = res);
    }
  }

  void _resetScan() {
    setState(() { _result = null; _error = null; _scannedCode = null; });
    _controller.start();
  }

  void _backToModePicker() {
    _controller.stop();
    setState(() { _mode = null; _result = null; _error = null; _scannedCode = null; });
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_mode == null) {
      return _buildModePicker();
    }
    return _buildScanner();
  }

  Widget _buildModePicker() {
    return Scaffold(
      appBar: AppBar(title: const Text('Scanner un colis')),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const SizedBox(height: 12),
            const Text('Que faites-vous ?',
                style: TextStyle(fontSize: 22, fontWeight: FontWeight.w800)),
            const SizedBox(height: 6),
            const Text('Choisissez l\'action avant de scanner le colis.',
                style: TextStyle(fontSize: 14, color: AppTheme.textMuted)),
            const SizedBox(height: 28),
            _ModeCard(
              icon: Icons.download_done,
              color: AppTheme.success,
              title: 'Dépôt',
              subtitle: 'Un client dépose un colis chez moi (envoi)\nou un transporteur livre un colis (réception)',
              onTap: () => _selectMode(ScanMode.depot),
            ),
            const SizedBox(height: 16),
            _ModeCard(
              icon: Icons.upload_outlined,
              color: AppTheme.accent,
              title: 'Récupération',
              subtitle: 'Un destinataire vient retirer son colis\n(code 4 chiffres demandé)',
              onTap: () => _selectMode(ScanMode.retrait),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildScanner() {
    final modeLabel = _mode == ScanMode.retrait ? 'Mode : Récupération' : 'Mode : Dépôt';
    final modeColor = _mode == ScanMode.retrait ? AppTheme.accent : AppTheme.success;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Scanner un colis'),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: _backToModePicker,
        ),
      ),
      body: Column(
        children: [
          Container(
            width: double.infinity,
            padding: const EdgeInsets.symmetric(vertical: 8, horizontal: 16),
            color: modeColor.withValues(alpha: 0.12),
            child: Text(modeLabel,
                textAlign: TextAlign.center,
                style: TextStyle(fontWeight: FontWeight.w700, color: modeColor)),
          ),
          Expanded(
            flex: 3,
            child: (_result != null || _error != null)
                ? _buildResult()
                : ClipRRect(
                    borderRadius: const BorderRadius.vertical(bottom: Radius.circular(20)),
                    child: MobileScanner(
                      controller: _controller,
                      onDetect: _onDetect,
                    ),
                  ),
          ),
          Expanded(
            flex: 1,
            child: Center(
              child: _processing
                  ? const Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        CircularProgressIndicator(),
                        SizedBox(height: 12),
                        Text('Traitement en cours...', style: TextStyle(fontWeight: FontWeight.w600)),
                      ],
                    )
                  : Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: const [
                        Icon(Icons.qr_code_scanner, size: 40, color: AppTheme.textMuted),
                        SizedBox(height: 8),
                        Text('Scannez le QR code du colis',
                            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
                        SizedBox(height: 4),
                        Text('Le statut sera mis à jour automatiquement',
                            style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                      ],
                    ),
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 8),
            child: SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                icon: const Icon(Icons.keyboard, size: 18),
                label: const Text('Saisir le code manuellement'),
                onPressed: _saisieManuelle,
              ),
            ),
          ),
        ],
      ),
    );
  }

  Future<void> _saisieManuelle() async {
    final codeCtrl = TextEditingController();
    final code = await showDialog<String>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Code colis'),
        content: TextField(
          controller: codeCtrl,
          decoration: const InputDecoration(labelText: 'CODE COLIS', hintText: 'COL-2026-0001'),
          textCapitalization: TextCapitalization.characters,
          autofocus: true,
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx), child: const Text('Annuler')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, codeCtrl.text.trim()),
            child: const Text('Valider'),
          ),
        ],
      ),
    );
    if (code == null || code.isEmpty || !mounted) return;
    setState(() { _scannedCode = code; _error = null; _result = null; _processing = true; });
    _processScan(code);
  }

  Widget _buildResult() {
    final isSuccess = _result != null;
    final action = _result?['action']?.toString() ?? '';
    final message = _result?['message']?.toString() ?? _error ?? '';
    final statut = _result?['statut']?.toString() ?? '';

    String title;
    IconData icon;
    Color color;

    if (isSuccess) {
      switch (action) {
        case 'depot_client':
          title = 'Dépôt confirmé';
          icon = Icons.download_done;
          color = AppTheme.success;
          break;
        case 'reception_relais':
          title = 'Colis réceptionné';
          icon = Icons.check_circle;
          color = AppTheme.success;
          break;
        case 'attente_retrait':
          title = 'En attente de retrait';
          icon = Icons.hourglass_top;
          color = AppTheme.accent;
          break;
        case 'refus':
          title = 'Colis refusé';
          icon = Icons.block;
          color = AppTheme.danger;
          break;
        default:
          title = 'Action effectuée';
          icon = Icons.check;
          color = AppTheme.success;
      }
    } else {
      title = 'Erreur';
      icon = Icons.error;
      color = AppTheme.danger;
    }

    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(32),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.05),
        borderRadius: const BorderRadius.vertical(bottom: Radius.circular(20)),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 64, color: color),
          const SizedBox(height: 16),
          Text(title, style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800, color: color)),
          const SizedBox(height: 8),
          if (_scannedCode != null)
            Text(_scannedCode!, style: const TextStyle(fontFamily: 'monospace', fontSize: 16, fontWeight: FontWeight.w600)),
          const SizedBox(height: 8),
          Text(message, style: const TextStyle(fontSize: 13, color: AppTheme.textMuted), textAlign: TextAlign.center),
          if (statut.isNotEmpty)
            Padding(
              padding: const EdgeInsets.only(top: 8),
              child: Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                decoration: BoxDecoration(
                  color: color.withValues(alpha: 0.15),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(statut, style: TextStyle(fontWeight: FontWeight.w700, color: color, fontSize: 12)),
              ),
            ),
          const SizedBox(height: 24),
          ElevatedButton.icon(
            icon: const Icon(Icons.qr_code_scanner, size: 18),
            label: const Text('Scanner un autre colis'),
            onPressed: _resetScan,
          ),
          const SizedBox(height: 8),
          TextButton(
            onPressed: _backToModePicker,
            child: const Text('Changer de mode'),
          ),
          if (_canShowRefuser())
            TextButton.icon(
              icon: const Icon(Icons.block, size: 16, color: AppTheme.danger),
              label: const Text('Refuser ce colis', style: TextStyle(color: AppTheme.danger)),
              onPressed: () => _refuserColis(_scannedCode!),
            ),
        ],
      ),
    );
  }

  /// Le bouton "Refuser" n'a de sens que si le colis a été trouvé et appartient à mon parcours.
  /// Si le scan a renvoyé une erreur (déjà refusé, déjà livré, ne passe pas par mon relais, etc.),
  /// on cache le bouton.
  bool _canShowRefuser() {
    if (_scannedCode == null) return false;
    if (_result != null && _result!['action']?.toString() == 'refus') return false;
    if (_error != null) {
      final err = _error!.toLowerCase();
      const motsBloquants = [
        'introuvable',
        'déjà refusé',
        'deja refuse',
        'déjà livré',
        'deja livre',
        'ne passe pas par votre',
      ];
      if (motsBloquants.any(err.contains)) return false;
    }
    return true;
  }

  Future<void> _refuserColis(String code) async {
    final motifCtrl = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Refuser le colis'),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                  'Le colis sera marqué comme refusé. L\'administration sera notifiée pour traiter le remboursement du client.',
                  style: TextStyle(fontSize: 13, color: AppTheme.textMuted)),
              const SizedBox(height: 12),
              TextField(
                controller: motifCtrl,
                minLines: 2,
                maxLines: 4,
                decoration: const InputDecoration(
                  labelText: 'MOTIF DU REFUS',
                  hintText: 'Ex : produit interdit, colis endommagé...',
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
          ElevatedButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: ElevatedButton.styleFrom(backgroundColor: AppTheme.danger),
            child: const Text('Confirmer'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;
    final motif = motifCtrl.text.trim();
    if (motif.length < 5) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Motif trop court (5 caractères minimum).')),
      );
      return;
    }

    final api = context.read<ApiService>();
    final res = await api.refuserColis(code, motif);
    if (!mounted) return;

    if (res.containsKey('error')) {
      setState(() => _error = res['error']);
    } else {
      setState(() {
        _result = {'action': 'refus', 'message': res['message'] ?? 'Colis refusé', 'statut': 'Refuse'};
        _error = null;
      });
    }
  }
}

class _ModeCard extends StatelessWidget {
  final IconData icon;
  final Color color;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  const _ModeCard({
    required this.icon,
    required this.color,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(14),
      child: Container(
        padding: const EdgeInsets.all(20),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.08),
          border: Border.all(color: color.withValues(alpha: 0.3), width: 1.5),
          borderRadius: BorderRadius.circular(14),
        ),
        child: Row(
          children: [
            Container(
              width: 56,
              height: 56,
              decoration: BoxDecoration(
                color: color,
                borderRadius: BorderRadius.circular(12),
              ),
              child: Icon(icon, color: Colors.white, size: 30),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title,
                      style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: color)),
                  const SizedBox(height: 4),
                  Text(subtitle,
                      style: const TextStyle(fontSize: 12, color: AppTheme.textMuted, height: 1.4)),
                ],
              ),
            ),
            Icon(Icons.chevron_right, color: color),
          ],
        ),
      ),
    );
  }
}
