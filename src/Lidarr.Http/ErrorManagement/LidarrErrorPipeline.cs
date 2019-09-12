﻿using System;
using System.Data.SQLite;
using FluentValidation;
using Nancy;
using NLog;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Exceptions;
using Lidarr.Http.Exceptions;
using Lidarr.Http.Extensions;
using HttpStatusCode = Nancy.HttpStatusCode;

namespace Lidarr.Http.ErrorManagement
{
    public class LidarrErrorPipeline
    {
        private readonly Logger _logger;

        public LidarrErrorPipeline(Logger logger)
        {
            _logger = logger;
        }

        public Response HandleException(NancyContext context, Exception exception)
        {
            _logger.Trace("Handling Exception");

            var apiException = exception as ApiException;

            if (apiException != null)
            {
                _logger.Warn(apiException, "API Error");
                return apiException.ToErrorResponse(context);
            }

            var validationException = exception as ValidationException;

            if (validationException != null)
            {
                _logger.Warn("Invalid request {0}", validationException.Message);

                return validationException.Errors.AsResponse(context, HttpStatusCode.BadRequest);
            }

            var clientException = exception as NzbDroneClientException;

            if (clientException != null)
            {
                return new ErrorModel
                {
                    Message = exception.Message,
                    Description = exception.ToString()
                }.AsResponse(context, (HttpStatusCode)clientException.StatusCode);
            }

            var sqLiteException = exception as SQLiteException;

            if (sqLiteException != null)
            {
                if (context.Request.Method == "PUT" || context.Request.Method == "POST")
                {
                    if (sqLiteException.Message.Contains("constraint failed"))
                        return new ErrorModel
                        {
                            Message = exception.Message,
                        }.AsResponse(context, HttpStatusCode.Conflict);
                }

                _logger.Error(sqLiteException, "[{0} {1}]", context.Request.Method, context.Request.Path);
            }

            _logger.Fatal(exception, "Request Failed. {0} {1}", context.Request.Method, context.Request.Path);

            return new ErrorModel
            {
                Message = exception.Message,
                Description = exception.ToString()
            }.AsResponse(context, HttpStatusCode.InternalServerError);
        }
    }
}
