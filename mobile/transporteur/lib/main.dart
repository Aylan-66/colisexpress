import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'services/api_service.dart';
import 'theme.dart';
import 'screens/login_screen.dart';
import 'screens/home_screen.dart';

final GlobalKey<NavigatorState> navigatorKey = GlobalKey<NavigatorState>();

void main() {
  runApp(const ColisExpressTransporteur());
}

class ColisExpressTransporteur extends StatelessWidget {
  const ColisExpressTransporteur({super.key});

  @override
  Widget build(BuildContext context) {
    return Provider<ApiService>(
      create: (_) => ApiService()..init(),
      child: MaterialApp(
        title: 'Colis Express Transporteur',
        theme: AppTheme.theme,
        debugShowCheckedModeBanner: false,
        navigatorKey: navigatorKey,
        home: const AuthGate(),
      ),
    );
  }
}

class AuthGate extends StatefulWidget {
  const AuthGate({super.key});

  @override
  State<AuthGate> createState() => _AuthGateState();
}

class _AuthGateState extends State<AuthGate> {
  bool _loading = true;
  bool _loggedIn = false;

  @override
  void initState() {
    super.initState();
    _checkAuth();
  }

  Future<void> _checkAuth() async {
    final api = context.read<ApiService>();
    await api.init();
    api.onSessionExpired = _onSessionExpired;
    setState(() {
      _loggedIn = api.isLoggedIn;
      _loading = false;
    });
  }

  void _onSessionExpired() {
    if (mounted) {
      setState(() => _loggedIn = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }
    return _loggedIn
        ? const HomeScreen()
        : LoginScreen(onLogin: () {
            final api = context.read<ApiService>();
            api.onSessionExpired = _onSessionExpired;
            setState(() => _loggedIn = true);
          });
  }
}
