import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import 'package:url_launcher/url_launcher.dart';
import '../config.dart';
import '../services/api_service.dart';
import '../theme.dart';

class ColisDetailScreen extends StatefulWidget {
  final String codeColis;
  const ColisDetailScreen({super.key, required this.codeColis});

  @override
  State<ColisDetailScreen> createState() => _ColisDetailScreenState();
}

class _ColisDetailScreenState extends State<ColisDetailScreen> {
  Map<String, dynamic>? _colis;
  bool _loading = true;
  String? _error;
  String? _success;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    final res = await context.read<ApiService>().getColisByCode(widget.codeColis);
    setState(() {
      _loading = false;
      if (res.containsKey('error')) {
        _error = res['error'];
      } else {
        _colis = res;
      }
    });
  }

  Future<void> _updateStatut(String statut, String commentaire) async {
    setState(() { _loading = true; _success = null; });
    final api = context.read<ApiService>();
    final res = await api.updateStatutColis(widget.codeColis, statut, commentaire);
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = 'Statut mis à jour : $statut');
      await _load();
    }
  }

  Future<void> _refuserColis() async {
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
            child: const Text('Confirmer le refus'),
          ),
        ],
      ),
    );

    if (confirmed != true || !mounted) return;
    final motif = motifCtrl.text.trim();
    if (motif.length < 5) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Motif trop court (5 caractères minimum).')),
        );
      }
      return;
    }

    setState(() { _loading = true; _success = null; _error = null; });
    final api = context.read<ApiService>();
    final res = await api.refuserColis(widget.codeColis, motif);
    if (!mounted) return;

    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = res['message']?.toString() ?? 'Colis refusé');
      await _load();
    }
  }

  /// Prise en charge avec photo obligatoire en une seule action.
  /// Si le colis est payé en espèces et pas encore encaissé, on demande d'abord la confirmation du paiement.
  /// Puis : passage à ReceptionneParTransporteur + upload photo (→ PhotoPriseEnChargeEnregistree).
  Future<void> _prendreEnChargeAvecPhoto() async {
    // Étape paiement espèces intégrée à la prise en charge
    final mode = _colis?['modeReglement']?.toString();
    final statutReglement = _colis?['statutReglement']?.toString();
    final estEspecesNonPaye = mode == 'Especes' && statutReglement != 'Paye';
    if (estEspecesNonPaye) {
      final montant = _colis?['total']?.toString() ?? '—';
      final okPaiement = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('Encaissement espèces'),
          content: Text('Ce colis est payé en espèces. Confirmez avoir reçu $montant € du client avant la prise en charge.'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Annuler')),
            ElevatedButton(
              style: ElevatedButton.styleFrom(backgroundColor: AppTheme.success),
              onPressed: () => Navigator.pop(ctx, true),
              child: const Text('Paiement reçu'),
            ),
          ],
        ),
      );
      if (okPaiement != true || !mounted) return;
    }

    final picker = ImagePicker();
    final photo = await picker.pickImage(source: ImageSource.camera, maxWidth: 1080, imageQuality: 70);
    if (photo == null) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Photo obligatoire pour la prise en charge.')),
        );
      }
      return;
    }

    setState(() { _loading = true; _success = null; _error = null; });
    final api = context.read<ApiService>();

    // 1) On passe d'abord à ReceptionneParTransporteur (sinon upload photo ne change rien)
    final commentaire = estEspecesNonPaye ? 'Prise en charge + paiement espèces encaissé' : 'Prise en charge';
    final updateRes = await api.updateStatutColis(widget.codeColis, 'ReceptionneParTransporteur', commentaire);
    if (updateRes.containsKey('error')) {
      setState(() { _error = updateRes['error']; _loading = false; });
      return;
    }
    // 2) Upload de la photo (passe à PhotoPriseEnChargeEnregistree)
    final res = await api.uploadPhotoColis(widget.codeColis, File(photo.path));
    if (!mounted) return;
    if (res.containsKey('error')) {
      setState(() { _error = res['error']; _loading = false; });
    } else {
      setState(() => _success = estEspecesNonPaye ? 'Paiement encaissé + colis pris en charge.' : 'Colis pris en charge avec photo.');
      await _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(widget.codeColis)),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && _colis == null
              ? Center(child: Text(_error!, style: const TextStyle(color: AppTheme.danger)))
              : _colis == null
                  ? const Center(child: Text('Colis introuvable'))
                  : RefreshIndicator(onRefresh: _load, child: _buildContent()),
    );
  }

  Widget _buildContent() {
    final statut = _colis!['statut']?.toString() ?? '—';
    final evenements = (_colis!['evenements'] as List?) ?? [];

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        if (_success != null)
          Container(
            padding: const EdgeInsets.all(12),
            margin: const EdgeInsets.only(bottom: 12),
            decoration: BoxDecoration(
              color: AppTheme.success.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(_success!, style: const TextStyle(color: AppTheme.success, fontWeight: FontWeight.w600)),
          ),
        if (_error != null)
          Container(
            padding: const EdgeInsets.all(12),
            margin: const EdgeInsets.only(bottom: 12),
            decoration: BoxDecoration(
              color: AppTheme.danger.withValues(alpha: 0.1),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text(_error!, style: const TextStyle(color: AppTheme.danger, fontWeight: FontWeight.w600)),
          ),

        // Info card
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text('${_colis!['villeDepart']} → ${_colis!['villeArrivee']}',
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800)),
                    ),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(
                        color: AppTheme.primary.withValues(alpha: 0.15),
                        borderRadius: BorderRadius.circular(6),
                      ),
                      child: Text(statut,
                          style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w700, color: AppTheme.primary)),
                    ),
                  ],
                ),
                const SizedBox(height: 12),
                _infoRow('Destinataire', _colis!['nomDestinataire'] ?? '—'),
                _infoRow('Poids déclaré', '${_colis!['poidsDeclare'] ?? 0} kg'),
                if (_colis!['dimensions'] != null) _infoRow('Dimensions', _colis!['dimensions']),
                _infoRow('Code retrait', _colis!['codeRetrait'] ?? '—'),
                if (_colis!['total'] != null)
                  _infoRow('Prix total', '${_colis!['total']} €'),
                if (_colis!['modeReglement'] != null)
                  _infoRow('Paiement', '${_colis!['modeReglement']} • ${_colis!['statutReglement']}'),
                if (_colis!['dateArriveeReelle'] != null)
                  _infoRow('Arrivée réelle', _fmtDateTime(_colis!['dateArriveeReelle']))
                else if (_colis!['dateArriveePrevue'] != null)
                  _infoRow('Arrivée prévue', _fmtDateTime(_colis!['dateArriveePrevue'])),
              ],
            ),
          ),
        ),

        const SizedBox(height: 12),

        // Actions
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Actions', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                const SizedBox(height: 4),
                Text(_actionsHint(statut), style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                const SizedBox(height: 12),
                Wrap(
                  spacing: 8,
                  runSpacing: 8,
                  children: _buildActions(statut),
                ),
              ],
            ),
          ),
        ),

        const SizedBox(height: 12),

        // Timeline
        Card(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Timeline', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800)),
                const SizedBox(height: 12),
                if (evenements.isEmpty)
                  const Text('Aucun événement', style: TextStyle(color: AppTheme.textMuted))
                else
                  ...evenements.map((e) => _timelineItem(e)),
              ],
            ),
          ),
        ),
      ],
    );
  }

  /// Ordre logique des statuts pour gérer les transitions autorisées
  int _ordre(String s) => const {
        'Brouillon': 0,
        'DemandeCreee': 1,
        'EnAttenteReglement': 2,
        'EnAttenteValidationTransporteur': 3,
        'ReservationConfirmee': 4,
        'CodeColisGenere': 5,
        'EnAttenteDepot': 6,
        'DeposeParClient': 7,
        'ReceptionneParTransporteur': 8,
        'PhotoPriseEnChargeEnregistree': 9,
        'EnTransit': 10,
        'ArriveDansPaysDest': 11,
        'ReceptionneParPointRelais': 12,
        'DisponibleAuRetrait': 13,
        'RetireParDestinataire': 14,
        'LivraisonCloturee': 15,
      }[s] ??
      -1;

  String _actionsHint(String statut) {
    final o = _ordre(statut);
    if (statut == 'Refuse') return 'Ce colis a été refusé.';
    if (statut == 'Annulee') return 'Cette commande a été annulée.';
    if (o >= 12) return 'Ce colis est au point relais — la suite est gérée par le relais.';
    if (o < 7) return 'En attente du dépôt du client au point relais.';
    if (o == 7) return 'Le client a déposé le colis. Prenez-le en charge (photo obligatoire).';
    if (o == 8 || o == 9) return 'Colis pris en charge. Vous pouvez le marquer "En transit".';
    if (o == 10) return 'Colis en transit. Marquez "Arrivé destination" à l\'arrivée.';
    if (o == 11) return 'Arrivé à destination. Le relais prendra le relais pour la mise à disposition.';
    return '';
  }

  List<Widget> _buildActions(String statut) {
    final o = _ordre(statut);
    final estFinal = statut == 'Refuse' || statut == 'Annulee' || statut == 'LivraisonCloturee' || statut == 'RetireParDestinataire';

    // Prise en charge : possible uniquement quand le client a déposé (DeposeParClient)
    final canPriseEnCharge = o == 7;
    // Pris en charge = a atteint au moins ReceptionneParTransporteur
    final prisEnCharge = o >= 8;
    // En transit : possible si pris en charge mais pas encore en transit
    final canEnTransit = prisEnCharge && o < 10;
    // Arrivé destination : possible uniquement si en transit
    final canArrive = o == 10;
    // Incident : possible une fois pris en charge et tant que pas livré
    final canIncident = prisEnCharge && o < 14 && !estFinal;
    // Refuser : uniquement AVANT prise en charge
    final canRefuser = !prisEnCharge && !estFinal && o >= 3;

    return [
      _actionButton('Prendre en charge (photo obligatoire)', null, Icons.handshake, AppTheme.primary,
          enabled: canPriseEnCharge, onTap: _prendreEnChargeAvecPhoto),
      _actionButton('En transit', 'EnTransit', Icons.local_shipping, AppTheme.primary, enabled: canEnTransit),
      _actionButton('Arrivé destination', 'ArriveDansPaysDest', Icons.flag, AppTheme.success, enabled: canArrive),
      _actionButton('Signaler incident', 'Incident', Icons.warning, AppTheme.danger, enabled: canIncident),
      _actionButton('Refuser le colis', null, Icons.block, AppTheme.danger, enabled: canRefuser, onTap: _refuserColis),
      // Reçu de dépôt disponible dès que le colis a été pris en charge
      _actionButton('Voir / partager le reçu', null, Icons.receipt, AppTheme.accent,
          enabled: prisEnCharge, onTap: _ouvrirRecu),
    ];
  }

  Future<void> _ouvrirRecu() async {
    final url = Uri.parse('${AppConfig.apiBaseUrl}/recu/${widget.codeColis}');
    if (!await launchUrl(url, mode: LaunchMode.externalApplication)) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Impossible d\'ouvrir le reçu.')),
      );
    }
  }

  Widget _actionButton(String label, String? statut, IconData icon, Color color, {bool enabled = true, VoidCallback? onTap}) {
    final c = enabled ? color : AppTheme.textMuted.withValues(alpha: 0.4);
    return SizedBox(
      width: double.infinity,
      child: OutlinedButton.icon(
        icon: Icon(icon, size: 18, color: c),
        label: Text(label, style: TextStyle(color: c)),
        style: OutlinedButton.styleFrom(
          foregroundColor: c,
          side: BorderSide(color: c.withValues(alpha: 0.3)),
          padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
        ),
        onPressed: !enabled
            ? null
            : () {
                if (onTap != null) {
                  onTap();
                  return;
                }
                if (statut != null) _updateStatut(statut, label);
              },
      ),
    );
  }

  String _fmtDateTime(dynamic iso) {
    if (iso == null) return '—';
    final d = DateTime.tryParse(iso.toString());
    if (d == null) return iso.toString();
    final l = d.toLocal();
    return '${l.day}/${l.month}/${l.year} ${l.hour}:${l.minute.toString().padLeft(2, '0')}';
  }

  Widget _infoRow(String label, String value) => Padding(
        padding: const EdgeInsets.only(bottom: 6),
        child: Row(
          children: [
            SizedBox(width: 120, child: Text(label, style: const TextStyle(color: AppTheme.textMuted, fontSize: 13))),
            Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600, fontSize: 13))),
          ],
        ),
      );

  Widget _timelineItem(Map<String, dynamic> e) {
    final statut = e['nouveauStatut']?.toString() ?? '—';
    final commentaire = e['commentaire']?.toString();
    final date = e['dateHeure']?.toString() ?? '';
    final d = DateTime.tryParse(date);
    final dateStr = d != null ? '${d.day}/${d.month}/${d.year} ${d.hour}:${d.minute.toString().padLeft(2, '0')}' : date;

    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(width: 10, height: 10,
                  decoration: BoxDecoration(shape: BoxShape.circle, color: AppTheme.primary)),
              Container(width: 2, height: 30, color: AppTheme.border),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(statut, style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
                if (commentaire != null && commentaire.isNotEmpty)
                  Text(commentaire, style: const TextStyle(fontSize: 12, color: AppTheme.textMuted)),
                Text(dateStr, style: const TextStyle(fontSize: 11, color: AppTheme.textMuted)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
