// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Collections.Generic;
using Barbatos.Wpf.Storage;

namespace Barbatos.Pallas.Wpf.Tests;

/// <summary>Preferences in a dictionary, which a test can read back.</summary>
internal sealed class FakePreferences : IPreferences
{
    public Dictionary<string, object?> Values { get; } = [];

    public bool ContainsKey(string key, string? sharedName = null) => Values.ContainsKey(key);

    public void Remove(string key, string? sharedName = null) => Values.Remove(key);

    public void Clear(string? sharedName = null) => Values.Clear();

    public void Set<T>(string key, T value, string? sharedName = null) => Values[key] = value;

    public T Get<T>(string key, T defaultValue, string? sharedName = null) =>
        Values.TryGetValue(key, out object? value) && value is T stored ? stored : defaultValue;
}
