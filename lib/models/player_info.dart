class PlayerInfo {
  final String nodeId;
  final String displayName;
  final String shipSkin;   // 'explorer', 'racer', 'hauler'
  final DateTime joinedAt;

  const PlayerInfo({
    required this.nodeId,
    required this.displayName,
    this.shipSkin = 'explorer',
    required this.joinedAt,
  });

  Map<String, dynamic> toJson() => {
    'nodeId': nodeId,
    'displayName': displayName,
    'shipSkin': shipSkin,
    'joinedAt': joinedAt.toIso8601String(),
  };

  factory PlayerInfo.fromJson(Map<String, dynamic> j) => PlayerInfo(
    nodeId: j['nodeId'] as String,
    displayName: j['displayName'] as String,
    shipSkin: j['shipSkin'] as String? ?? 'explorer',
    joinedAt: DateTime.parse(j['joinedAt'] as String),
  );
}
