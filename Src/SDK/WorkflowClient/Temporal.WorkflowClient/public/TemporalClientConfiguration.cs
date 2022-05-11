﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Temporal.Api.WorkflowService.V1;
using Temporal.Serialization;
using Temporal.WorkflowClient.Interceptors;

namespace Temporal.WorkflowClient
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="ServiceConnection">
    /// </param>
    /// <param name="CustomGrpcWorkflowServiceClient">
    ///   <c>CustomGrpcWorkflowServiceClient</c> and <c>ServiceConnection</c> are mutually exclusive.
    /// </param>
    /// <param name="Namespace">
    /// </param>
    /// <param name="ClientIdentityMarker">
    /// </param>
    /// <param name="PayloadConverterFactory">
    ///   Factory receives the `IWorkflowHandle` for which the payload converter is being constructed and returns
    ///   a new non-null data converter to be applied to all calls made by the client for the specific `IWorkflowHandle` instance.
    /// </param>
    /// <param name="PayloadCodecFactory">
    ///   Factory receives the `IWorkflowHandle` for which the payload codec is being constructed and returns
    ///   a new non-null payload converter to be applied to all calls made by the client for the specific `IWorkflowHandle` instance.
    /// </param>
    /// <param name="ClientInterceptorFactory">
    ///   Factory receives the `IWorkflowHandle` for which the interceptors are being constructed and list of all
    ///   already existing interceptors, i.e. the interceptors generated by the system. That received interceptor
    ///   list is never null, but may be empty. The interceptor list does NOT include the final "sink" interceptor
    ///   - that will always remain LAST and the factory shall not be able to affect that. However, the list DOES
    ///   include all other system interceptors, if any (e.g. the ones that implement distributed tracing).
    ///   The factory may modify that list any way it wants, including removing system interceptors (expect
    ///   the aforementioned sink) or adding new interceptors before or after.
    ///   Nulls must not be added to the list.
    /// </param>
    public partial record TemporalClientConfiguration(
                                TemporalClientConfiguration.Connection ServiceConnection,
                                WorkflowService.WorkflowServiceClient CustomGrpcWorkflowServiceClient,
                                string Namespace,
                                string ClientIdentityMarker,
                                Func<ServiceInvocationPipelineItemFactoryArguments, IPayloadConverter> PayloadConverterFactory,
                                Func<ServiceInvocationPipelineItemFactoryArguments, IPayloadCodec> PayloadCodecFactory,
                                Action<ServiceInvocationPipelineItemFactoryArguments, IList<ITemporalClientInterceptor>> ClientInterceptorFactory)
    {

        #region Static APIs

        /// <summary>
        /// Creates a new <c>TemporalClientConfiguration</c> initialized with default settings for use with
        /// a local Temporal server installation.<br/>
        /// To create a <c>TemporalClientConfiguration</c> with custom settings, use the ctor.
        /// </summary>
        public static TemporalClientConfiguration ForLocalHost()
        {
            return new TemporalClientConfiguration();
        }

        /// <summary>
        /// Creates a new <c>TemporalClientConfiguration</c> initialized with default settings for use with
        /// a the Temporal Cloud frontend.<br/>
        /// To create a <c>TemporalClientConfiguration</c> with custom settings, use the ctor.
        /// </summary>
        /// <param name="namespace">The Temporal namespace to use by the clients based on the configuration being created.</param>
        public static TemporalClientConfiguration ForTemporalCloud(string @namespace, string clientCertPemFilePath, string clientKeyPemFilePath)
        {
            Temporal.Util.Validate.NotNull(@namespace);

            return new TemporalClientConfiguration()
            {
                ServiceConnection = TemporalClientConfiguration.Connection.ForTemporalCloud(@namespace,
                                                                                            clientCertPemFilePath,
                                                                                            clientKeyPemFilePath),
                Namespace = @namespace,
                ClientIdentityMarker = null
            };
        }

#if NETCOREAPP3_1_OR_GREATER
        /// <summary>
        /// Creates a new <c>TemporalClientConfiguration</c> initialized with default settings for use with
        /// a the Temporal Cloud frontend.<br/>
        /// To create a <c>TemporalClientConfiguration</c> with custom settings, use the ctor.
        /// </summary>
        /// <remarks>
        /// This overload is only available on Net Core and Net 5+. See <see cref="TemporalClientConfiguration.TlsCertificate"/>
        /// for details on this topic.
        /// </remarks>
        public static TemporalClientConfiguration ForTemporalCloud(string @namespace, X509Certificate2 clientCert)
        {
            Temporal.Util.Validate.NotNull(@namespace);

            return new TemporalClientConfiguration()
            {
                ServiceConnection = TemporalClientConfiguration.Connection.ForTemporalCloud(@namespace, clientCert),
                Namespace = @namespace,
                ClientIdentityMarker = null
            };
        }
#endif

        public static void Validate(TemporalClientConfiguration config)
        {
            Temporal.Util.Validate.NotNull(config);

            if (config.ServiceConnection != null && config.CustomGrpcWorkflowServiceClient != null)
            {
                throw new ArgumentException($"`{nameof(ServiceConnection)}` and `{nameof(CustomGrpcWorkflowServiceClient)}` are"
                                          + $" mutually exclusive. Exactly one of these values must be set, but both are non-null.");
            }
            else if (config.ServiceConnection == null && config.CustomGrpcWorkflowServiceClient == null)
            {
                throw new ArgumentException($"One of `{nameof(ServiceConnection)}` or `{nameof(CustomGrpcWorkflowServiceClient)}` must"
                                          + $" be set. However, both are null.");
            }
            else if (config.CustomGrpcWorkflowServiceClient == null)
            {
                TemporalClientConfiguration.Connection.Validate(config.ServiceConnection);
            }
            else
            {
                // So `config.ServiceConnection` is null and `config.CustomGrpcWorkflowServiceClient` is not null.
                // Validate `config.CustomGrpcWorkflowServiceClient` here if we discover any requirements.
            }

            TemporalClientConfiguration.Connection.Validate(config.ServiceConnection);
            Temporal.Util.Validate.NotNullOrWhitespace(config.Namespace);

            if (config.ClientIdentityMarker != null && String.IsNullOrWhiteSpace(config.ClientIdentityMarker))
            {
                throw new ArgumentException($"{nameof(config)}.{nameof(config.ClientIdentityMarker)} must be either not set (=null) or it"
                                          + $" must be a non-whitespace-only string, however, \"{config.ClientIdentityMarker}\" was specified.");
            }

            // `PayloadConverterFactory` may or may not be null;
            // `PayloadCodecFactory` may or may not be null;
            // `ClientInterceptorFactory` may or may not be null;
        }

        #endregion Static APIs

        public TemporalClientConfiguration()
            : this(ServiceConnection: new TemporalClientConfiguration.Connection(),
                   CustomGrpcWorkflowServiceClient: null,
                   Namespace: "default",
                   ClientIdentityMarker: null,
                   PayloadConverterFactory: null,
                   PayloadCodecFactory: null,
                   ClientInterceptorFactory: null)
        {
        }
    }
}
