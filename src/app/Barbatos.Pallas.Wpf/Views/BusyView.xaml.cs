// This Source Code Form is subject to the terms of the MIT License.
// If a copy of the MIT was not distributed with this file, You can obtain one at https://opensource.org/licenses/MIT.
// Copyright (C) Barbatos Labs | Pham The Hung and Barbatos.Pallas Contributors.
// All Rights Reserved.

using System.Windows.Controls;
using Barbatos.Pallas.Presentation;

namespace Barbatos.Pallas.Wpf.Views;

/// <summary>
/// The cover over the screen while the session calculates, and what it says once the calculation has run a while.
/// </summary>
/// <remarks>
/// Its data context is the session's <see cref="SessionWork"/>. It takes every click from the moment a work starts, so
/// that nothing on the screen reads or changes the session the work has; it is seen only after
/// <c>BusyDelay</c>, with AC to stop the calculation.
/// </remarks>
public partial class BusyView : UserControl
{
    /// <summary>Creates the view.</summary>
    public BusyView()
    {
        InitializeComponent();
    }
}
