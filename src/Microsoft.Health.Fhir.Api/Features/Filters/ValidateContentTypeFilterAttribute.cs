﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Net.Http;
using EnsureThat;
using Hl7.Fhir.Rest;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Primitives;
using Microsoft.Health.Fhir.Api.Features.ContentTypes;
using Microsoft.Health.Fhir.Api.Features.Routing;
using Microsoft.Health.Fhir.Core.Exceptions;
using Microsoft.Net.Http.Headers;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Health.Fhir.Api.Features.Filters
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class ValidateContentTypeFilterAttribute : ActionFilterAttribute
    {
        private readonly IContentTypeService _contentTypeService;

        public ValidateContentTypeFilterAttribute(IContentTypeService contentTypeService)
        {
            EnsureArg.IsNotNull(contentTypeService, nameof(contentTypeService));

            _contentTypeService = contentTypeService;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            EnsureArg.IsNotNull(context, nameof(context));

            HttpContext contextHttpContext = context.HttpContext;

            await _contentTypeService.CheckRequestedContentTypeAsync(contextHttpContext);

            // If the request is a put or post and has a content-type, check that it's supported
            if (contextHttpContext.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase) ||
                contextHttpContext.Request.Method.Equals(HttpMethod.Put.Method, StringComparison.InvariantCultureIgnoreCase))
            {
                if (contextHttpContext.Request.Headers.TryGetValue(HeaderNames.ContentType, out StringValues headerValue))
                {
                    var resourceFormat = ContentType.GetResourceFormatFromContentType(headerValue[0]);

                    if (!await _contentTypeService.IsFormatSupportedAsync(resourceFormat) && context.ActionDescriptor?.AttributeRouteInfo?.Name != RouteNames.SearchResourcesPost)
                    {
                        throw new UnsupportedMediaTypeException(Resources.UnsupportedContentTypeHeader);
                    }
                }
                else
                {
                    // If no content type is supplied, then the server should respond with an unsupported media type exception.
                    throw new UnsupportedMediaTypeException(Resources.ContentTypeHeaderRequired);
                }
            }

            await base.OnActionExecutionAsync(context, next);
        }
    }
}