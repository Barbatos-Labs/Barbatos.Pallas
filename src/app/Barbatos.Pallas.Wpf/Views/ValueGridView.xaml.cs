// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// A grid of values, as every form of the calculator is made of.
/// </summary>
/// <remarks>Its data context is a <see cref="Presentation.ValueGridViewModel"/>.</remarks>
public partial class ValueGridView : UserControl
{
    /// <summary>Creates the control.</summary>
    public ValueGridView()
    {
        InitializeComponent();
    }
}
