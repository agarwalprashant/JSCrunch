﻿using System.Linq;
using FluentAssertions;
using JSCrunch.Core;
using JSCrunch.Core.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace JSCrunch.Tests
{
    [TestClass]
    public class WhenTheFileChangedEventIsReceived
    {
        private EventQueue _eventQueue;
        private FileChangedEventListener _listener;

        [TestInitialize]
        public void Initialize()
        {
            _eventQueue = new EventQueue();
            _listener = new FileChangedEventListener(_eventQueue, new DummyConfigurator());
        }

        [TestMethod]
        public void ThenATestRunStartedEventIsPublished()
        {
            _listener.Publish(new FileChangedEvent("c:\\temp\\somefile.js"));

            _eventQueue
                .OfType<TestRunStartedEvent>()
                .Should()
                .HaveCount(1, "there should be a TestRunStartedEvent");
        }
    }
}