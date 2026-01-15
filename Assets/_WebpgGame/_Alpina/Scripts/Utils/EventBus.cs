using System;
// Evento con parametros o datos
public static class EventBus<T> {
    public static event Action<T> OnEvent;

    public static void Publish(T data) => OnEvent?.Invoke(data);
}

// Eventos que no necesitan parametros o datos
public static class EventBus {
    public static event Action OnEvent;
    public static void Publish() => OnEvent?.Invoke();
}
public struct PlayerExitZoneSignal {
    public UnityEngine.GameObject player;
}