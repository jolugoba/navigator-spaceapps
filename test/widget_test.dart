import 'package:flutter_test/flutter_test.dart';
import 'package:navigator/main.dart';

void main() {
  testWidgets('NavigatorApp smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const NavigatorApp());
    expect(find.text('NAVIGATOR'), findsWidgets);
  });
}
