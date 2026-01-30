using System;

public interface IObservable {
  void Subscribe(Action<object> handler);
}

/// <summary>
/// Mark method as observable handler
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ObservableHandlerAttribute : Attribute {
  public ObservableHandlerAttribute() { }
}

/// <summary>
/// Generic observable class that notifies subscribers on value changes
///
/// NOTE: Supports only built-in and struct like types
/// </summary>
public class Observable<T> : IObservable where T : IEquatable<T> {
  private T _value;

  public T Value {
    get => _value;
    set {
      _value = value;
      IssueOnChangeHandlers();
    }
  }

  public delegate void OnChangeHandler(T value);
  public event OnChangeHandler OnChange;
  public void IssueOnChangeHandlers() {
    if (OnChange == null) return;
    foreach (OnChangeHandler callback in OnChange.GetInvocationList()) {
      callback(Value);
    }
  }

  public Observable(T initialValue) {
    _value = initialValue;
  }

  public void Subscribe(Action<object> handler) {
    OnChange += (value) => handler(value);
  }

  // Implicit conversion for convenience
  public static implicit operator Observable<T>(T value) => new(value);

  public override string ToString() => _value.ToString();
}
