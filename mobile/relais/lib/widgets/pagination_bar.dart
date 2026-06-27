import 'package:flutter/material.dart';

/// Barre de sélection du nombre d'éléments par page (à mettre en haut).
class PerPageSelector extends StatelessWidget {
  final int value;
  final List<int> options;
  final int totalItems;
  final ValueChanged<int> onChanged;

  const PerPageSelector({
    super.key,
    required this.value,
    required this.totalItems,
    required this.onChanged,
    this.options = const [10, 20, 50, 100],
  });

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
      child: Row(
        children: [
          const Text('Par page :', style: TextStyle(fontSize: 12, fontWeight: FontWeight.w600)),
          const SizedBox(width: 8),
          DropdownButton<int>(
            value: value,
            isDense: true,
            underline: const SizedBox.shrink(),
            items: options
                .map((n) => DropdownMenuItem(value: n, child: Text('$n', style: const TextStyle(fontSize: 13))))
                .toList(),
            onChanged: (v) { if (v != null) onChanged(v); },
          ),
          const Spacer(),
          Text('$totalItems total', style: const TextStyle(fontSize: 11, color: Color(0xFF78716C))),
        ],
      ),
    );
  }
}

/// Contrôles de pagination à mettre en bas de la liste : ‹ Page X / Y ›.
class PaginationControls extends StatelessWidget {
  final int currentPage; // 1-based
  final int totalPages;
  final ValueChanged<int> onPageChanged;

  const PaginationControls({
    super.key,
    required this.currentPage,
    required this.totalPages,
    required this.onPageChanged,
  });

  @override
  Widget build(BuildContext context) {
    if (totalPages <= 1) return const SizedBox.shrink();
    final canPrev = currentPage > 1;
    final canNext = currentPage < totalPages;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 16),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          IconButton(
            icon: const Icon(Icons.first_page),
            onPressed: canPrev ? () => onPageChanged(1) : null,
            tooltip: 'Première page',
          ),
          IconButton(
            icon: const Icon(Icons.chevron_left),
            onPressed: canPrev ? () => onPageChanged(currentPage - 1) : null,
          ),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
            decoration: BoxDecoration(
              color: const Color(0xFF1B4965).withValues(alpha: 0.08),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Text('Page $currentPage / $totalPages',
                style: const TextStyle(fontWeight: FontWeight.w700, fontSize: 13)),
          ),
          IconButton(
            icon: const Icon(Icons.chevron_right),
            onPressed: canNext ? () => onPageChanged(currentPage + 1) : null,
          ),
          IconButton(
            icon: const Icon(Icons.last_page),
            onPressed: canNext ? () => onPageChanged(totalPages) : null,
            tooltip: 'Dernière page',
          ),
        ],
      ),
    );
  }
}
