using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;
using Temporal.Api.History.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Util;
using Temporal.WorkflowClient;

namespace Temporal.TestUtil
{
    internal sealed class ExtendedWorkflowServiceClient : IDisposable
    {
        private int _isDisposed = 0;
        private readonly WorkflowServiceClientEnvelope _workflowServiceClientEnvelope = null;
        private readonly TemporalClientConfiguration _clientConfig;

        public ExtendedWorkflowServiceClient(TemporalClientConfiguration clientConfig)
        {
            Validate.NotNull(clientConfig);

            _clientConfig = clientConfig;
            _workflowServiceClientEnvelope = WorkflowServiceClientFactory.SingletonInstance.GetOrCreateClient(clientConfig.ServiceConnection);
            _workflowServiceClientEnvelope.AddRef();
        }

        public void Dispose()
        {
            if (0 == Interlocked.Exchange(ref _isDisposed, 1))
            {
                _workflowServiceClientEnvelope.Release();
            }
        }

        private WorkflowService.WorkflowServiceClient Client
        {
            get { return _workflowServiceClientEnvelope?.GrpcWorkflowServiceClient; }
        }

        private const int MaxConseqEmptyRetiesDefault = 2;
        public async Task<List<HistoryEvent>> GetHistoryAsync(string workflowId,
                                                              int maxLength = -1,
                                                              int maxConseqEmptyReties = MaxConseqEmptyRetiesDefault)
        {
            Validate.NotNull(workflowId);

            List<HistoryEvent> history = new();
            ByteString nextPageToken = ByteString.Empty;
            int conseqEmptyReties = 0;

            while (true)
            {
                // Formulate request:
                GetWorkflowExecutionHistoryRequest reqGetWfExHist = new()
                {
                    Namespace = _clientConfig.Namespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = workflowId,
                        RunId = String.Empty,
                    },
                    NextPageToken = nextPageToken,
                    WaitNewEvent = true,
                    HistoryEventFilterType = HistoryEventFilterType.AllEvent,
                };

                // Execute remte call:
                GetWorkflowExecutionHistoryResponse resGetWfExHist = await Client.GetWorkflowExecutionHistoryAsync(reqGetWfExHist,
                                                                                                                   headers: null,
                                                                                                                   deadline: null);

                // Track consecutive times where we get no data and give up, so that the test does not hang.
                if (resGetWfExHist.History.Events.Count > 0)
                {
                    conseqEmptyReties = 0;
                }
                else
                {
                    conseqEmptyReties++;
                    if (maxConseqEmptyReties > 0 && conseqEmptyReties == maxConseqEmptyReties)
                    {
                        throw new Exception($"GetWorkflowExecutionHistory returned no events for {conseqEmptyReties} consecutive calls."
                                          + $" The the specified maximum `{nameof(maxConseqEmptyReties)}` is reached."
                                          + $" Giving up.");
                    }
                }

                // Add all received events to history:
                foreach (HistoryEvent histEvent in resGetWfExHist.History.Events)
                {
                    history.Add(histEvent);

                    // If we collected the requested number of events - stop.
                    if (maxLength > 0 && maxLength == history.Count)
                    {
                        return history;
                    }

                    // If event is terminal - stop:
                    if (histEvent.EventType == EventType.WorkflowExecutionCompleted
                            || histEvent.EventType == EventType.WorkflowExecutionFailed
                            || histEvent.EventType == EventType.WorkflowExecutionTimedOut
                            || histEvent.EventType == EventType.WorkflowExecutionCanceled
                            || histEvent.EventType == EventType.WorkflowExecutionTerminated
                            || histEvent.EventType == EventType.WorkflowExecutionContinuedAsNew
                            || histEvent.EventType == EventType.WorkflowExecutionFailed
                            || histEvent.EventType == EventType.WorkflowExecutionFailed
                            || histEvent.EventType == EventType.WorkflowExecutionFailed
                            || histEvent.EventType == EventType.WorkflowExecutionFailed
                            || histEvent.EventType == EventType.WorkflowExecutionFailed)
                    {
                        return history;
                    }
                }

                // If we have a next page token and we have not yet reached any of the above done-conditions, make another call.
                // Otherwise give up.
                if (resGetWfExHist.NextPageToken != null && resGetWfExHist.NextPageToken.Length > 0)
                {
                    nextPageToken = resGetWfExHist.NextPageToken;
                }
                else
                {
                    return history;
                }
            }
        }
    }
}
