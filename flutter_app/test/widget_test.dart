import 'package:flutter_test/flutter_test.dart';
import 'package:shared_preferences/shared_preferences.dart';

import 'package:navigator_app/main.dart';
import 'package:navigator_app/src/mission_log.dart';
import 'package:navigator_app/src/unity_host.dart';

void main() {
  setUp(() => SharedPreferences.setMockInitialValues({}));

  testWidgets('home shows title, launch button and switches language', (tester) async {
    await tester.pumpWidget(const NavigatorApp());
    await tester.pumpAndSettle();
    expect(find.text('NAVIGATOR'), findsOneWidget);
    expect(find.text('START MISSION'), findsOneWidget);

    await tester.tap(find.text('ESPAÑOL'));
    await tester.pumpAndSettle();
    expect(find.text('INICIAR MISIÓN'), findsOneWidget);
  });

  test('parses a mission message from Unity and stores it', () async {
    const msg = '{"type":"mission","planet":"mars","objective":"orbit","score":1234,'
        '"arrived":true,"position":1,"stars":2,"costM":480,"timeS":61.5,"fuel":0.25,'
        '"science":300,"ts":1790000000000}';
    final json = UnityHost.parse(msg)!;
    expect(json['type'], 'mission');

    await MissionLog.add(MissionRecord.fromJson(json));
    final log = await MissionLog.load();
    expect(log, hasLength(1));
    expect(log.first.planet, 'mars');
    expect(log.first.score, 1234);
    expect(log.first.arrived, isTrue);
    expect(log.first.stars, 2);
  });

  test('ignores messages that are not JSON objects', () {
    expect(UnityHost.parse('touch'), isNull);
    expect(UnityHost.parse('[1,2]'), isNull);
  });
}
