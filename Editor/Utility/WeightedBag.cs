using System.Collections.Generic;

public sealed class WeightedBag<TKey, TValue> {
    public readonly Dictionary<TKey, Result> Results = new Dictionary<TKey, Result>();

    public void Add(TKey key, TValue value) {
        if (Results.TryGetValue(key, out var result)) {
            result.value.Add(value);
            result.counter++;
        } else {
            Results.Add(key, new Result { value = new List<TValue> { value }, counter = 1 });
        }
    }

    public struct Result {
        public int counter;
        public List<TValue> value;
    }
}