﻿using Microsoft.VisualStudio.Shell.Interop;

namespace JSCrunch.VisualStudio.Events
{
    public class SolutionOpenedEvent : JSCrunch.Core.Events.Event
    {
        public SolutionOpenedEvent(IVsSolution solution)
        {
            Solution = solution;
        }

        public IVsSolution Solution { get; private set; }
    }
}