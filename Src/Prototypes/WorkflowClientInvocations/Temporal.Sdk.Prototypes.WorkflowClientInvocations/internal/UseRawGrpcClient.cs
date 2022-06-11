using System;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

#if NETCOREAPP3_1
using System.Security.Cryptography;
#endif

using Google.Protobuf;
using Grpc.Core;

using Temporal.Api.Common.V1;
using Temporal.Api.Enums.V1;
using Temporal.Api.History.V1;
using Temporal.Api.TaskQueue.V1;
using Temporal.Api.Workflow.V1;
using Temporal.Api.WorkflowService.V1;
using Temporal.Util;

#if NETCOREAPP
using Microsoft.Extensions.Logging;
#endif

namespace Temporal.Demos.AdHocScenarios
{
    internal class UseRawGrpcClient
    {
        private const string TemporalServerHost = "localhost";
        //private const string TemporalServerHost = "NAME.ACCNT.tmprl.cloud";
        private const int TemporalServerPort = 7233;

        private const string TestNamespace = "default";
        //private const string TestNamespace = "NAME.ACCNT";


        public void Run()
        {
            Console.WriteLine();

            //SignalWorkflowAsync().GetAwaiter().GetResult();
            //DescribeWorkflowAsync().GetAwaiter().GetResult();

            ListWorkflowExecutionsAsync().GetAwaiter().GetResult();
            StartWorkflowAsync().GetAwaiter().GetResult();
            //WaitForWorkflowAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("Done. Press Enter.");
            Console.ReadLine();
        }

        private WorkflowService.WorkflowServiceClient CreateClientNetFx()
        {
#if NETFRAMEWORK

            // *** Use this for non-secured connections {
            //Grpc.Core.Channel channel = new Grpc.Core.Channel(TemporalServerHost, TemporalServerPort, ChannelCredentials.Insecure);
            // *** }

            // *** Use this for TLS connections {
            //string clientCertData = File.ReadAllText(@"PATH\NAME.crt.pem");
            //string clientKeyData = File.ReadAllText(@"PATH\NAME.key.pem");

            // For mTLS:
            //SslCredentials sslCreds = new(rootCertificates: null,
            //                              new KeyCertificatePair(certificateChain: clientCertData,
            //                                                     privateKey: clientKeyData),
            //                              verifyPeerCallback: VerifyPeerCallback);

            //string clientCertData = File.ReadAllText(@"PATH\client.pem");
            //string clientKeyData = File.ReadAllText(@"PATH\client.key");

            string serverCertData = File.ReadAllText(@"PATH\ca - Copy.cert.crt");

            SslCredentials sslCreds = new(serverCertData,
                                          keyCertificatePair: null,
                                          verifyPeerCallback: null);

            ChannelOption[] channelOptions = new ChannelOption[]
            {
                //new ChannelOption(ChannelOptions.SslTargetNameOverride, "tls-sample"),
            };

            Grpc.Core.Channel channel = new(TemporalServerHost, TemporalServerPort, sslCreds, channelOptions);
            // *** }

            Console.WriteLine($"Created a `{channel.GetType().FullName}` to \"{channel.ResolvedTarget}\".");

            WorkflowService.WorkflowServiceClient client = new(channel);
            return client;
#else
            throw new NotSupportedException("This routine is only supported on Net Fx.");
#endif
        }

        private static bool VerifyPeerCallback(VerifyPeerContext context)
        {
            Console.WriteLine($"Invoked {nameof(VerifyPeerCallback)}(..):");
            Console.WriteLine($"    {nameof(VerifyPeerCallback)}.TargetName:{Format.QuoteOrNull(context?.TargetName)}");
            Console.WriteLine($"    {nameof(VerifyPeerCallback)}.PeerPem:{Format.QuoteOrNull(context?.PeerPem)}");

            return true;
        }

        private WorkflowService.WorkflowServiceClient CreateClientNetCore()
        {
#if NETCOREAPP
            if (!RuntimeEnvironmentInfo.SingletonInstance.CoreAssembyInfo.IsSysPrivCoreLib)
            {
                throw new InvalidOperationException("RuntimeEnvironmentInfo.SingeltonInstance.CoreAssembyInfo.IsSysPrivCoreLib was expected to be True.");
            }

            if (RuntimeEnvironmentInfo.SingletonInstance.RuntimeVersion.StartsWith("3"))
            {
                AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            }

            ILoggerFactory logFactory = LoggerFactory.Create((logBuilder) =>
            {
                ConsoleLoggerExtensions.AddConsole(logBuilder);
                logBuilder.SetMinimumLevel(LogLevel.Trace);
            });

            // *** Use this for non-secured connections {
            //Grpc.Net.Client.GrpcChannel channel = Grpc.Net.Client.GrpcChannel.ForAddress($"http://{TemporalServerHost}:{TemporalServerPort}",
            //                                                                             new Grpc.Net.Client.GrpcChannelOptions()
            //                                                                             {
            //                                                                                 //LoggerFactory = logFactory
            //                                                                             });
            // *** }

            // *** Use this for TLS connections {
            HttpClientHandler httpClientHandler = new();

            if (TemporalServerHost.Equals(TemporalServerHost, StringComparison.OrdinalIgnoreCase))
            {
                // Required for self-signed certs:
                //httpClientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                httpClientHandler.ServerCertificateCustomValidationCallback = ServerCertificateCustomValidationCallback;
            }

            // Required only for mTLS {
            //string clientCertData = File.ReadAllText(@"PATH\NAME.crt.pem");
            //string clientKeyData = File.ReadAllText(@"PATH\NAME.key.pem");

            //string clientCertData = File.ReadAllText(@"PATH\client.pem");
            //string clientKeyData = File.ReadAllText(@"PATH\client.key");

            //httpClientHandler.ClientCertificates.Add(clientCert);

            // } Required only for mTLS

            Grpc.Net.Client.GrpcChannel channel = Grpc.Net.Client.GrpcChannel.ForAddress($"https://{TemporalServerHost}:{TemporalServerPort}",
                                                                                         new Grpc.Net.Client.GrpcChannelOptions()
                                                                                         {
                                                                                             //LoggerFactory = logFactory,
                                                                                             HttpHandler = httpClientHandler,
                                                                                         });
            // *** }

            Console.WriteLine($"Created a `{channel.GetType().FullName}` to \"{channel.Target}\".");

            WorkflowService.WorkflowServiceClient client = new(channel);
            return client;
#else
            throw new NotSupportedException("This routine is only supported on Net Core and Net 5+.");
#endif
        }

        private static bool ServerCertificateCustomValidationCallback(HttpRequestMessage httpRequestMessage,
                                                                      X509Certificate2 cert,
                                                                      X509Chain chain,
                                                                      SslPolicyErrors policyErrors)
        {
            Console.WriteLine($"Invoked {nameof(ServerCertificateCustomValidationCallback)}(..):");
            Console.WriteLine($"    {nameof(policyErrors)}={policyErrors}");

            string caCertData = File.ReadAllText(@"PATH\ca - Copy.cert.crt");
            X509Certificate2 caCert = CreateX509CertFromData(caCertData, keyMarkedUpData: null);

            chain = chain ?? new X509Chain();

            chain.ChainPolicy.ExtraStore.Add(caCert);

            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.VerificationFlags |= X509VerificationFlags.AllowUnknownCertificateAuthority;

            bool IsValidBuild = chain.Build(cert);
            Console.WriteLine($"    {nameof(IsValidBuild)}={IsValidBuild}");

            if (!IsValidBuild)
            {
                return false;
            }

            X509Certificate2 chainEnd = chain.ChainElements[chain.ChainElements.Count - 1].Certificate;

            bool caMatch = DataEqual(caCert.RawData, chainEnd.RawData);
            Console.WriteLine($"    {nameof(caMatch)}={caMatch}");

            return caMatch;
        }

        private static bool DataEqual(byte[] data1, byte[] data2)
        {
            if (Object.ReferenceEquals(data1, data2))
            {
                return true;
            }

            if (data1 == null || data2 == null)
            {
                return false;
            }

            if (data1.Length != data2.Length)
            {
                return false;
            }

            for (int i = 0; i < data1.Length; i++)
            {
                if (data1[i] != data2[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static X509Certificate2 CreateX509CertFromData(string certMarkedUpData, string keyMarkedUpData)
        {
            // Get the ephemeral (in-memory) cert:

            using X509Certificate2 ephemeralCert = CreateX509EphemeralCertFromData(certMarkedUpData, keyMarkedUpData);

            // Work around Windows ephemeral cert bugs:
            // (https://github.com/natemcmaster/LettuceEncrypt/pull/110)
            // (https://stackoverflow.com/questions/55456807/create-x509certificate2-from-cert-and-key-without-making-a-pfx-file)
            // (https://github.com/dotnet/runtime/issues/23749)
            // @ToDo: Review this for other OSes when supported.

            //X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.Exportable;
            X509KeyStorageFlags keyStorageFlags = X509KeyStorageFlags.DefaultKeySet;
#if NETCOREAPP3_1_OR_GREATER
            keyStorageFlags |= X509KeyStorageFlags.EphemeralKeySet;
#endif

            byte[] ephemeralCertBytes = ephemeralCert.Export(X509ContentType.Pfx);
            X509Certificate2 certificate = new(rawData: ephemeralCertBytes,
                                               password: (string) null,
                                               keyStorageFlags: keyStorageFlags);

            // Done:    
            return certificate;
        }

        /// <summary>
        /// The `X509Certificate2.CreateFromPem(..)` API was not available before Net 6.
        /// This method implements the respective logic for older Framework versions and delegates to the Framework API where possible.
        /// Inspired by 
        ///   https://github.com/grpc/grpc-dotnet/blob/dd72d6a38ab2984fd224aa8ed53686dc0153b9da/testassets/InteropTestsClient/InteropClient.cs#L898-L918
        /// and
        ///   https://stackoverflow.com/questions/7400500/how-to-get-private-key-from-pem-file/10498045#10498045
        /// </summary>        
        private static X509Certificate2 CreateX509EphemeralCertFromData(string certMarkedUpData, string keyMarkedUpData)
        {
#if NET6_0_OR_GREATER            
            return (keyMarkedUpData == null)
                        ? X509Certificate2.CreateFromPem(certMarkedUpData)
                        : X509Certificate2.CreateFromPem(certMarkedUpData, keyMarkedUpData);
#elif NETCOREAPP3_1_OR_GREATER
            // Create public cert:

            byte[] certBytes = GetPemSectionContent(certMarkedUpData, "CERTIFICATE");
            X509Certificate2 pubCert = new(certBytes);

            if (keyMarkedUpData == null)
            {
                return pubCert;
            }

            // Add private key to get complete cert:

            try
            {
                ExceptionAggregator exAggr = new();
                using RSA rsa = RSA.Create();
                bool privKeyLoaded = false;

                try
                {
                    byte[] keyBytes = GetPemSectionContent(keyMarkedUpData, "PRIVATE KEY");
                    rsa.ImportPkcs8PrivateKey(keyBytes, out _);
                    privKeyLoaded = true;
                }
                catch (Exception ex)
                {
                    exAggr.Add(ex);
                }

                if (!privKeyLoaded)
                {
                    try
                    {
                        byte[] keyBytes = GetPemSectionContent(keyMarkedUpData, "RSA PRIVATE KEY");
                        rsa.ImportRSAPrivateKey(keyBytes, out _);
                        privKeyLoaded = true;
                    }
                    catch (Exception ex)
                    {
                        exAggr.Add(ex);
                    }
                }

                if (!privKeyLoaded)
                {
                    exAggr.ThrowIfNotEmpty();
                    throw new InvalidOperationException($"Could not read the private key from {nameof(keyMarkedUpData)}.");
                }

                X509Certificate2 ephemeralCert = pubCert.CopyWithPrivateKey(rsa);
                return ephemeralCert;
            }
            finally
            {
                pubCert.Dispose();
            }
#else
            throw new NotSupportedException("This method is not supported under the targeted .NET version.");
#endif
        }

        private static byte[] GetPemSectionContent(string markedUpPem, string sectionName)
        {
            const string SectionStartMarkerTemplate = "-----BEGIN {0}-----";
            const string SectionEndMarkerTemplate = "-----END {0}-----";

            string sectionStartMarker = String.Format(SectionStartMarkerTemplate, sectionName);
            string sectionEndMarker = String.Format(SectionEndMarkerTemplate, sectionName);

            int sectionDataStartIndex = markedUpPem.IndexOf(sectionStartMarker, StringComparison.Ordinal);
            if (sectionDataStartIndex < 0)
            {
                throw new FormatException($"Cannot find the start of the specified section (Marker=\"{sectionStartMarker}\").");
            }

            sectionDataStartIndex += sectionStartMarker.Length;

            int sectionDataEndIndex = markedUpPem.IndexOf(sectionEndMarker, sectionDataStartIndex, StringComparison.Ordinal)
                                    - sectionDataStartIndex;

            if (sectionDataEndIndex < 0)
            {
                throw new FormatException($"Cannot find the end of the specified section"
                                        + $" (SearchStartIndex={sectionDataStartIndex}; Marker=\"{sectionEndMarker}\").");
            }

            string sectionContentEncodedBytes = markedUpPem.Substring(sectionDataStartIndex, sectionDataEndIndex);
            return Convert.FromBase64String(sectionContentEncodedBytes);
        }

        private WorkflowService.WorkflowServiceClient CreateClient()
        {
            return RuntimeEnvironmentInfo.SingletonInstance.CoreAssembyInfo.IsMscorlib
                        ? CreateClientNetFx()
                        : CreateClientNetCore();
        }

        public async Task ListWorkflowExecutionsAsync()
        {
            Console.WriteLine("\n----------- Workflow ListWorkflowExecutionsAsync { ----------- -----------\n");

            WorkflowService.WorkflowServiceClient client = CreateClient();

            ListWorkflowExecutionsRequest reqListExecs = new()
            {
                Namespace = TestNamespace,
            };

            ListWorkflowExecutionsResponse resListExecs = await client.ListWorkflowExecutionsAsync(reqListExecs);

            foreach (WorkflowExecutionInfo weInfo in resListExecs.Executions)
            {
                Console.WriteLine($"WorkflowId=\"{weInfo.Execution.WorkflowId}\";"
                                + $" RunId=\"{weInfo.Execution.RunId}\";"
                                + $" TypeName=\"{weInfo.Type.Name}\""
                                + $" Status=\"{weInfo.Status}\"");
            }

            Console.WriteLine();
            Console.WriteLine("----------- } Workflow ListWorkflowExecutionsAsync ----------- -----------");
            Console.WriteLine();
        }

        public async Task StartWorkflowAsync()
        {
            Console.WriteLine("\n----------- Workflow StartWorkflowAsync { ----------- -----------\n");

            WorkflowService.WorkflowServiceClient client = CreateClient();

            StartWorkflowExecutionRequest reqStartWf = new()
            {
                Namespace = TestNamespace,
                WorkflowId = "Some-Workflow-Id",
                WorkflowType = new WorkflowType()
                {
                    Name = "Some-Workflow-Name",
                },
                TaskQueue = new TaskQueue()
                {
                    Name = "Some-Task-Queue",
                },
                RequestId = Guid.NewGuid().ToString(),
            };

            AsyncUnaryCall<StartWorkflowExecutionResponse> calStartWf = client.StartWorkflowExecutionAsync(reqStartWf);

            try
            {
                StartWorkflowExecutionResponse resStartWf = await calStartWf;
                Console.WriteLine($"Workflow Execution started. RunId = \"{resStartWf.RunId}\".");
            }
            catch (RpcException rpcEx)
            {
                Console.WriteLine($"Could not start Workflow Execution. {rpcEx}");
            }

            Console.WriteLine("\n----------- } Workflow StartWorkflowAsync ----------- -----------\n");
        }

        public async Task WaitForWorkflowAsync()
        {
            const string DemoWorkflowId = "Some-Workflow-Id";

            Console.WriteLine();
            Console.WriteLine("\n----------- Workflow WaitForWorkflowAsync { ----------- -----------\n");

            try
            {
                await WaitForWorkflowAsync(DemoWorkflowId, workflowRunId: String.Empty);
                Console.WriteLine($"Workflow result received successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting workflow result. {ex}");
            }

            Console.WriteLine("\n----------- } Workflow WaitForWorkflowAsync ----------- -----------\n");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0010:Add missing cases", Justification = "Only need to process terminal events.")]
        private async Task WaitForWorkflowAsync(string workflowId, string workflowRunId)
        {
            WorkflowService.WorkflowServiceClient client = CreateClient();

            const string ServerCallDescription = "GetWorkflowExecutionHistoryAsync(..) with HistoryEventFilterType = CloseEvent";
            ByteString nextPageToken = ByteString.Empty;

            // Spin and retry or follow the workflow chain until we get a result, or hit a non-retriable error, or time out:
            while (true)
            {

                GetWorkflowExecutionHistoryRequest reqGWEHist = new()
                {
                    Namespace = TestNamespace,
                    Execution = new WorkflowExecution()
                    {
                        WorkflowId = workflowId,
                        RunId = workflowRunId
                    },
                    NextPageToken = nextPageToken,
                    WaitNewEvent = true,
                    HistoryEventFilterType = HistoryEventFilterType.CloseEvent,
                };

                GetWorkflowExecutionHistoryResponse resGWEHist = await client.GetWorkflowExecutionHistoryAsync(reqGWEHist);

                if (resGWEHist.History.Events.Count == 0 && resGWEHist.NextPageToken != null && resGWEHist.NextPageToken.Length > 0)
                {
                    Console.WriteLine("Received no history events, but a non-empty NextPageToken. Repeating call.");
                    nextPageToken = resGWEHist.NextPageToken;
                    continue;
                }

                if (resGWEHist.History.Events.Count != 1)
                {
                    throw new UnexpectedServerResponseException(ServerCallDescription,
                                                                nameof(WaitForWorkflowAsync),
                                                                $"History.Events.Count was expected to be 1, but it was in fact {resGWEHist.History.Events.Count}");
                }

                HistoryEvent historyEvent = resGWEHist.History.Events[0];

                switch (historyEvent.EventType)
                {
                    case EventType.WorkflowExecutionCompleted:
                        Console.WriteLine("Workflow completed succsessfully.");
                        return;

                    case EventType.WorkflowExecutionFailed:
                        Console.WriteLine($"Workflow execution failed ({historyEvent.WorkflowExecutionFailedEventAttributes.Failure.Message}).");
                        return;

                    case EventType.WorkflowExecutionTimedOut:
                        Console.WriteLine($"Workflow execution timed out.");
                        return;

                    case EventType.WorkflowExecutionCanceled:
                        Console.WriteLine($"Workflow execution cancelled.");
                        return;

                    case EventType.WorkflowExecutionTerminated:
                        Console.WriteLine($"Workflow execution terminated (reason=\"{historyEvent.WorkflowExecutionTerminatedEventAttributes.Reason}\").");
                        return;

                    case EventType.WorkflowExecutionContinuedAsNew:
                        string nextRunId = historyEvent.WorkflowExecutionContinuedAsNewEventAttributes.NewExecutionRunId;

                        if (String.IsNullOrWhiteSpace(nextRunId))
                        {
                            throw new UnexpectedServerResponseException(ServerCallDescription,
                                                                        nameof(WaitForWorkflowAsync),
                                                                        $"EventType was WorkflowExecutionContinuedAsNew, but NewExecutionRunId=\"{nextRunId}\"");
                        }

                        Console.WriteLine($"Workflow execution continued as new. Following workflow chain to RunId=\"{nextRunId}\"");
                        workflowRunId = nextRunId;
                        continue;

                    default:
                        throw new UnexpectedServerResponseException(ServerCallDescription,
                                                                    nameof(WaitForWorkflowAsync),
                                                                    $"Unexpected History EventType (\"{historyEvent.EventType}\")");
                }
            }  // while(true)
        }

        public async Task DescribeWorkflowAsync()
        {
            Console.WriteLine("\n----------- Workflow DescribeWorkflowAsync { ----------- -----------\n");

            WorkflowService.WorkflowServiceClient client = CreateClient();

            DescribeWorkflowExecutionRequest reqDesrcWfExecWf = new()
            {
                Namespace = TestNamespace,
                Execution = new WorkflowExecution()
                {
                    WorkflowId = "qqq", // String.Empty,
                    RunId = "f47f5aa0-8740-4c40-b4df-b40c44c5f068",
                }
            };

            DescribeWorkflowExecutionResponse resDesrcWfExecWf;
            try
            {
                resDesrcWfExecWf = await client.DescribeWorkflowExecutionAsync(reqDesrcWfExecWf);
                Console.WriteLine($"Workflow Execution Described.");
            }
            catch (RpcException rpcEx)
            {
                Console.WriteLine($"Could not Describe Workflow Execution. {rpcEx}");
                return;
            }

            Console.WriteLine($"WorkflowId: \"{resDesrcWfExecWf.WorkflowExecutionInfo.Execution.WorkflowId}\".");
            Console.WriteLine($"RunId: \"{resDesrcWfExecWf.WorkflowExecutionInfo.Execution.RunId}\".");
            Console.WriteLine($"TypeName: \"{resDesrcWfExecWf.WorkflowExecutionInfo.Type.Name}\".");

            Console.WriteLine("\n----------- } Workflow DescribeWorkflowAsync ----------- -----------\n");
        }

        public async Task SignalWorkflowAsync()
        {
            Console.WriteLine("\n----------- Workflow SignalWorkflowAsync { ----------- -----------\n");

            WorkflowService.WorkflowServiceClient client = CreateClient();

            SignalWorkflowExecutionRequest reqSignalWf = new()
            {
                Namespace = TestNamespace,
                WorkflowExecution = new WorkflowExecution()
                {
                    WorkflowId = "qqq", // String.Empty,
                    RunId = "f47f5aa0-8740-4c40-b4df-b40c44c5f068",
                },
                SignalName = "Dummy-Signal",
                RequestId = Guid.NewGuid().ToString(),
            };

            SignalWorkflowExecutionResponse resSignalWf;
            try
            {
                resSignalWf = await client.SignalWorkflowExecutionAsync(reqSignalWf);
                Console.WriteLine($"Workflow Signalled.");
            }
            catch (RpcException rpcEx)
            {
                Console.WriteLine($"Could not Signal Workflow Execution. {rpcEx}");
                return;
            }

            Console.WriteLine("\n----------- } Workflow SignalWorkflowAsync ----------- -----------\n");
        }

        public class UnexpectedServerResponseException : Exception
        {
            private static string FormatMessage(string serverCall, string scenario, string problemDescription)
            {
                StringBuilder message = new();

                if (!String.IsNullOrWhiteSpace(problemDescription))
                {
                    message.Append(problemDescription);
                }

                if (!String.IsNullOrWhiteSpace(problemDescription))
                {
                    if (message.Length > 0 && message[message.Length - 1] != '.')
                    {
                        if (message[message.Length - 1] != '.')
                        {
                            message.Append('.');
                        }

                        message.Append(' ');
                    }

                    message.Append("Server Call: \"");
                    message.Append(serverCall);
                    message.Append("\".");
                }

                if (!String.IsNullOrWhiteSpace(scenario))
                {
                    if (message.Length > 0 && message[message.Length - 1] != '.')
                    {
                        if (message[message.Length - 1] != '.')
                        {
                            message.Append('.');
                        }

                        message.Append(' ');
                    }

                    message.Append("Scenario: \"");
                    message.Append(scenario);
                    message.Append("\".");
                }

                return message.ToString();
            }

            public UnexpectedServerResponseException(string serverCall, string scenario, string problemDescription)
                : base(FormatMessage(serverCall, scenario, problemDescription))
            {
            }

            public UnexpectedServerResponseException(string serverCall, string scenario, Exception unexpectedException)
                : base(FormatMessage(serverCall, scenario, $"Unexpected exception occurred ({unexpectedException.GetType().Name})"))
            {
            }
        }
    }
}
