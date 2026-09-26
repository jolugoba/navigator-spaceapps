import 'components.dart';
import 'systems.dart';

class SpaceWorld {
  int _nextId = 1;
  final Map<EntityId, Map<Type, Component>> _store = {};
  final List<System> _systems = [];

  EntityId createEntity() {
    final id = _nextId++;
    _store[id] = {};
    return id;
  }

  void removeEntity(EntityId id) => _store.remove(id);

  void add<T extends Component>(EntityId id, T component) {
    _store[id]?[T] = component;
  }

  T? get<T extends Component>(EntityId id) =>
      _store[id]?[T] as T?;

  bool has<T extends Component>(EntityId id) =>
      _store[id]?.containsKey(T) ?? false;

  Iterable<EntityId> query<T extends Component>() =>
      _store.entries
          .where((e) => e.value.containsKey(T))
          .map((e) => e.key);

  void addSystem(System system) => _systems.add(system);

  void tick(double dt) {
    for (final system in _systems) {
      system.tick(this, dt);
    }
  }

  void dispose() {
    _systems.clear();
    _store.clear();
  }

  // Snapshot de naves para sincronización
  List<EntityId> get allShips =>
      query<ShipOwner>().toList();

  EntityId? get localShip {
    for (final id in query<ShipOwner>()) {
      if (get<ShipOwner>(id)!.isLocal) return id;
    }
    return null;
  }
}
