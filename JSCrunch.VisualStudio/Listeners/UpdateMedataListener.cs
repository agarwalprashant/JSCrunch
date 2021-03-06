﻿using System.Linq;
using JSCrunch.Core;
using JSCrunch.Core.Events;
using JSCrunch.VisualStudio.Events;
using JSCrunch.VisualStudio.Metadata;
using Microsoft.VisualStudio.Shell.Interop;

namespace JSCrunch.VisualStudio.Listeners
{
    public class UpdateMedataListener : ISubscribable<UpdateMetadataEvent>
    {
        private readonly MetadataModel _model;
        private readonly EventQueue _eventQueue;

        public UpdateMedataListener(MetadataModel model, EventQueue eventQueue)
        {
            _model = model;
            _eventQueue = eventQueue;
        }

        public void Publish(UpdateMetadataEvent eventInstance)
        {
            var dirty = false;

            if (eventInstance is SolutionOpenedEvent)
            {
                var solutionOpenedEvent = (SolutionOpenedEvent)eventInstance;
                object pvar;
                solutionOpenedEvent.Solution.GetProperty((int)__VSPROPID.VSPROPID_SolutionBaseName, out pvar);
                var solutionName = (string)pvar;

                if (_model.SolutionName != solutionName)
                {
                    _model.SolutionName = solutionName;
                    _model.Projects.Clear();

                    dirty = true;
                }
            }

            if (eventInstance is ProjectLoadedEvent)
            {
                var projectLoadedEvent = (ProjectLoadedEvent)eventInstance;

                var projectExists = _model.Projects.Any(c => c.Name == projectLoadedEvent.Project.GetProjectName());
                if (!projectExists)
                {
                    _model
                        .Projects
                        .Add(new ProjectModel { Name = projectLoadedEvent.Project.GetProjectName() });

                    dirty = true;
                }
            }

            if (eventInstance is TestsFoundEvent)
            {
                var testsFoundEvent = (TestsFoundEvent)eventInstance;

                var project = _model.Projects.SingleOrDefault(c => c.Name == testsFoundEvent.ProjectName);
                if (project != null)
                {
                    foreach (var test in testsFoundEvent.Tests)
                    {
                        var exists = project.Tests.Any(c => c.Name == test.Name);
                        if (!exists)
                        {
                            project
                                .Tests
                                .Add(new TestModel { Name = test.Name });

                            dirty = true;
                        }
                    }
                }
            }

            if (eventInstance is TestResultsAvailableEvent)
            {
                var testResults = eventInstance as TestResultsAvailableEvent;

                // Try to find the test file in the model
                foreach (var project in _model.Projects)
                {
                    var testModel = project.Tests.SingleOrDefault(t => t.Name == testResults.TestSuite + ".ts");

                    if (testModel != null)
                    {
                        // Update status
                        testModel.NumberOfFailures = testResults.NumberOfFailures;
                        testModel.Tests = testResults.Tests.Select(t => new ResultModel
                        {
                            Name = t.Name,
                            Success = t.Success,
                            Output = t.Output
                        }).ToList();

                        dirty = true;
                    }
                }
            }

            if (dirty)
            {
                _eventQueue.Enqueue(new MetadataChangedEvent((MetadataModel)_model.Clone()));
            }
        }
    }
}