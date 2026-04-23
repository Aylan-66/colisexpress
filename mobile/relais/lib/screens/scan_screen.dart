import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';

class ScanScreen extends StatefulWidget {
  const ScanScreen({super.key});

  @override
  State<ScanScreen> createState() => _ScanScreenState();
}

class _ScanScreenState extends State<ScanScreen> {
  final MobileScannerController _controller = MobileScannerController();
  bool _processing = false;
  Map<String, dynamic>? _result;
  String? _error;
  String? _scannedCode;

  void _onDetect(BarcodeCapture capture) {
    if (_processing) return;
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
    final res = await api.scanColis(code);

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
            const Text('Confirmez que vous avez reçu le paiement en espèces avant de scanner le colis.',
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
            child: const Text('Paiement reçu ✓'),
          ),
        ],
      ),
    );

    if (confirmed == true && commandeId != null && mounted) {
      final api = context.read<ApiService>();
      // Valider le paiement
      await api.validerPaiementEspeces(commandeId);
      // Re-scanner maintenant que c'est payé
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

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Scanner un colis')),
      body: Column(
        children: [
          // Caméra
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

          // Instructions
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
                      children: [
                        Icon(Icons.qr_code_scanner, size: 40, color: AppTheme.textMuted),
                        const SizedBox(height: 8),
                        const Text('Scannez le QR code du colis',
                            style: TextStyle(fontSize: 15, fontWeight: FontWeight.w700)),
                        const SizedBox(height: 4),
                        const Text('Le statut sera mis à jour automatiquement',
                            style: TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                      ],
                    ),
            ),
          ),
        ],
      ),
    );
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
          title = 'Dépôt client confirmé';
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
        ],
      ),
    );
  }
}
