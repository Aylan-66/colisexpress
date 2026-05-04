import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:provider/provider.dart';
import '../services/api_service.dart';
import '../theme.dart';
import 'colis_detail_screen.dart';

class EtapeDetailScreen extends StatefulWidget {
  final String trajetId;
  final String etapeId;
  final String relaisNom;

  const EtapeDetailScreen({
    super.key,
    required this.trajetId,
    required this.etapeId,
    required this.relaisNom,
  });

  @override
  State<EtapeDetailScreen> createState() => _EtapeDetailScreenState();
}

class _EtapeDetailScreenState extends State<EtapeDetailScreen> {
  Map<String, dynamic>? _data;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _loading = true);
    final res = await context.read<ApiService>().getColisForEtape(widget.trajetId, widget.etapeId);
    setState(() {
      _loading = false;
      if (!res.containsKey('error')) _data = res;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.relaisNom)),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _data == null
              ? const Center(child: Text('Erreur chargement'))
              : RefreshIndicator(
                  onRefresh: _load,
                  child: ListView(
                    padding: const EdgeInsets.all(16),
                    children: [
                      // Header
                      Card(
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Row(
                            children: [
                              Icon(_typeIcon, size: 32, color: _typeColor),
                              const SizedBox(width: 14),
                              Expanded(
                                child: Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(_data!['relais'] ?? '', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                                    Text('${_data!['ville']} — ${_typeLabel}',
                                        style: TextStyle(color: _typeColor, fontWeight: FontWeight.w600, fontSize: 13)),
                                  ],
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),

                      const SizedBox(height: 16),

                      // À récupérer
                      if (_type != 'arrivee') ...[
                        _sectionHeader('À RÉCUPÉRER', Icons.upload, AppTheme.primary, _aRecuperer.length),
                        if (_aRecuperer.isEmpty)
                          _emptyCard('Aucun colis à récupérer à cet arrêt')
                        else
                          ..._aRecuperer.map((c) => _colisCard(c, isDeposer: false)),
                        const SizedBox(height: 16),
                      ],

                      // À déposer
                      if (_type != 'depart') ...[
                        _sectionHeader('À DÉPOSER', Icons.download, AppTheme.accent, _aDeposer.length),
                        if (_aDeposer.isEmpty)
                          _emptyCard('Aucun colis à déposer à cet arrêt')
                        else
                          ..._aDeposer.map((c) => _colisCard(c, isDeposer: true)),
                      ],

                      // Si premier arrêt, montrer aussi les colis à déposer plus loin
                      if (_type == 'depart' && _aDeposer.isNotEmpty) ...[
                        const SizedBox(height: 16),
                        _sectionHeader('COLIS DESTINATION FINALE', Icons.flag, AppTheme.success, _aDeposer.length),
                        ..._aDeposer.map((c) => _colisCard(c, isDeposer: true)),
                      ],
                    ],
                  ),
                ),
    );
  }

  String get _type => _data?['type']?.toString() ?? 'intermediaire';
  List<dynamic> get _aDeposer => (_data?['aDeposer'] as List?) ?? [];
  List<dynamic> get _aRecuperer => (_data?['aRecuperer'] as List?) ?? [];

  Color get _typeColor => switch (_type) {
        'depart' => AppTheme.primary,
        'arrivee' => AppTheme.success,
        _ => AppTheme.accent,
      };

  IconData get _typeIcon => switch (_type) {
        'depart' => Icons.flight_takeoff,
        'arrivee' => Icons.flight_land,
        _ => Icons.swap_vert,
      };

  String get _typeLabel => switch (_type) {
        'depart' => 'Point de départ',
        'arrivee' => 'Destination finale',
        _ => 'Arrêt intermédiaire',
      };

  Widget _sectionHeader(String title, IconData icon, Color color, int count) => Padding(
        padding: const EdgeInsets.only(bottom: 10),
        child: Row(
          children: [
            Icon(icon, size: 18, color: color),
            const SizedBox(width: 8),
            Text(title, style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: color, letterSpacing: 1)),
            const Spacer(),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(color: color.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(10)),
              child: Text('$count', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w800, color: color)),
            ),
          ],
        ),
      );

  Widget _emptyCard(String msg) => Card(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: Center(child: Text(msg, style: const TextStyle(color: AppTheme.textMuted, fontSize: 13))),
        ),
      );

  Future<void> _scanEtValider(String codeAttendu, String statut, String commentaire) async {
    final scanned = await Navigator.push<String>(context,
        MaterialPageRoute(builder: (_) => _ScanVerifScreen(codeAttendu: codeAttendu)));
    if (scanned == null || !mounted) return;

    final api = context.read<ApiService>();
    final res = await api.updateStatutColis(scanned, statut, commentaire);
    if (!mounted) return;
    if (res.containsKey('error')) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(res['error']), backgroundColor: Colors.red),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('$scanned → $commentaire'), backgroundColor: Colors.green),
      );
      _load();
    }
  }

  Widget _colisCard(Map<String, dynamic> c, {required bool isDeposer}) {
    final code = c['codeColis']?.toString() ?? '—';
    final dest = c['nomDestinataire']?.toString() ?? '—';
    final ville = c['villeDestinataire']?.toString() ?? '—';
    final poids = c['poidsDeclare']?.toString() ?? '—';
    final statut = c['statut']?.toString() ?? '';

    // Déterminer si le colis est actionnable
    final bool isActionnable;
    final String statutLabel;
    final Color statutColor;

    if (isDeposer) {
      // À déposer : actionnable si le transporteur l'a (ReceptionneParTransporteur, EnTransit, etc.)
      isActionnable = statut == 'ReceptionneParTransporteur' || statut == 'PhotoPriseEnChargeEnregistree' || statut == 'EnTransit';
      if (isActionnable) { statutLabel = 'Prêt à déposer'; statutColor = AppTheme.accent; }
      else { statutLabel = statut; statutColor = AppTheme.textMuted; }
    } else {
      // À récupérer : actionnable si le client l'a déposé (DeposeParClient)
      isActionnable = statut == 'DeposeParClient';
      if (statut == 'DeposeParClient') { statutLabel = 'Déposé — prêt'; statutColor = AppTheme.success; }
      else if (statut == 'EnAttenteDepot' || statut == 'EnAttenteReglement') { statutLabel = 'En attente de dépôt'; statutColor = AppTheme.textMuted; }
      else if (statut == 'ReceptionneParTransporteur') { statutLabel = 'Déjà récupéré'; statutColor = AppTheme.success; }
      else { statutLabel = statut; statutColor = AppTheme.textMuted; }
    }

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: Opacity(
        opacity: isActionnable ? 1.0 : 0.5,
        child: Column(
          children: [
            ListTile(
              onTap: isActionnable ? () => Navigator.push(context,
                  MaterialPageRoute(builder: (_) => ColisDetailScreen(codeColis: code))) : null,
              contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
              leading: Container(
                width: 42, height: 42,
                decoration: BoxDecoration(
                  color: (isActionnable ? (isDeposer ? AppTheme.accent : AppTheme.primary) : AppTheme.textMuted).withValues(alpha: 0.1),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(isDeposer ? Icons.download : Icons.upload,
                    color: isActionnable ? (isDeposer ? AppTheme.accent : AppTheme.primary) : AppTheme.textMuted, size: 20),
              ),
              title: Text(code, style: const TextStyle(fontWeight: FontWeight.w700, fontFamily: 'monospace', fontSize: 13)),
              subtitle: Text('$dest — $ville • $poids kg', style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
              trailing: Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(color: statutColor.withValues(alpha: 0.15), borderRadius: BorderRadius.circular(6)),
                child: Text(statutLabel, style: TextStyle(fontSize: 10, fontWeight: FontWeight.w700, color: statutColor)),
              ),
            ),
            if (isActionnable)
              Padding(
                padding: const EdgeInsets.fromLTRB(14, 0, 14, 10),
                child: Row(
                  children: [
                    if (!isDeposer) ...[
                      // Récupérer = change le statut à ReceptionneParTransporteur
                      Expanded(
                        child: ElevatedButton.icon(
                          icon: const Icon(Icons.qr_code_scanner, size: 18),
                          label: const Text('Prendre en charge'),
                          style: ElevatedButton.styleFrom(backgroundColor: AppTheme.primary, padding: const EdgeInsets.symmetric(vertical: 10)),
                          onPressed: () => _scanEtValider(code, 'ReceptionneParTransporteur', 'Pris en charge par le transporteur'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Expanded(
                        child: OutlinedButton.icon(
                          icon: const Icon(Icons.block, size: 18, color: AppTheme.danger),
                          label: const Text('Refuser', style: TextStyle(color: AppTheme.danger)),
                          style: OutlinedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 10)),
                          onPressed: null,
                        ),
                      ),
                    ],
                    if (isDeposer)
                      Expanded(
                        child: ElevatedButton.icon(
                          icon: const Icon(Icons.download_done, size: 18),
                          label: const Text('Déposer au relais'),
                          style: ElevatedButton.styleFrom(backgroundColor: AppTheme.accent, padding: const EdgeInsets.symmetric(vertical: 10)),
                          onPressed: () => _scanEtValider(code, 'ArriveDansPaysDest', 'Colis déposé en point relais par le transporteur'),
                        ),
                      ),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }
}

class _ScanVerifScreen extends StatefulWidget {
  final String codeAttendu;
  const _ScanVerifScreen({required this.codeAttendu});

  @override
  State<_ScanVerifScreen> createState() => _ScanVerifScreenState();
}

class _ScanVerifScreenState extends State<_ScanVerifScreen> {
  final MobileScannerController _ctrl = MobileScannerController();
  bool _done = false;

  void _onDetect(BarcodeCapture capture) {
    if (_done) return;
    final code = capture.barcodes.firstOrNull?.rawValue;
    if (code == null) return;

    _done = true;
    _ctrl.stop();

    if (code == widget.codeAttendu) {
      Navigator.pop(context, code);
    } else {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Mauvais colis'),
          content: Text('Code scanné : $code\nAttendu : ${widget.codeAttendu}\n\nScannez le bon QR code.'),
          actions: [
            TextButton(onPressed: () { Navigator.pop(ctx); setState(() => _done = false); _ctrl.start(); }, child: const Text('Réessayer')),
            TextButton(onPressed: () { Navigator.pop(ctx); Navigator.pop(context); }, child: const Text('Annuler')),
          ],
        ),
      );
    }
  }

  @override
  void dispose() { _ctrl.dispose(); super.dispose(); }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Scanner ${widget.codeAttendu}')),
      body: Column(
        children: [
          Expanded(
            flex: 3,
            child: MobileScanner(controller: _ctrl, onDetect: _onDetect),
          ),
          Expanded(
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text('Scannez le QR du colis', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                  const SizedBox(height: 4),
                  Text(widget.codeAttendu, style: const TextStyle(fontFamily: 'monospace', fontSize: 18, fontWeight: FontWeight.w800, color: AppTheme.primary)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
