﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using Ticket.SaleWebApi.Application;

namespace Ticket.SaleWebApi.Controllers
{
    /// <summary>
    /// 携程 http://otaapi.fengjing.com:8086/otaapi/xiecheng/orderhandler
    /// </summary>
    [RoutePrefix("api/ctrip")]
    public class CtripController : ApiController
    {
        private readonly CtripFacadeService _ctripFacadeService;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctripFacadeService"></param>
        public CtripController(CtripFacadeService ctripFacadeService)
        {
            _ctripFacadeService = ctripFacadeService;
        }

        /// <summary>
        /// 携程
        /// </summary>
        /// <response code="200">The user got.</response>
        /// <response code="404">The user not found.</response>
        [Route("handler")]
        public HttpResponseMessage PostHandler()
        {
            string request = Request.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(request))
            {
                return new HttpResponseMessage
                {
                    Content = new StringContent(""),
                    StatusCode = HttpStatusCode.OK
                };
            }
            var result = _ctripFacadeService.Handler(request);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(result, Encoding.UTF8, "text/xml");
            return response;
        }
    }
}
