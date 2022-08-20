/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.IO;
using System.Net;

namespace Facebook.WitAi
{
    public class WrapHttpWebRequest : IRequest
    {
        HttpWebRequest _httpWebRequest;

        public WrapHttpWebRequest(HttpWebRequest httpWebRequest)
        {
            _httpWebRequest = httpWebRequest;
        }

        public WebHeaderCollection Headers { get => _httpWebRequest.Headers; set => _httpWebRequest.Headers = value; }
        public string Method { get => _httpWebRequest.Method; set => _httpWebRequest.Method = value; }
        public string ContentType { get => _httpWebRequest.ContentType; set => _httpWebRequest.ContentType = value; }
        public long ContentLength { get => _httpWebRequest.ContentLength; set => _httpWebRequest.ContentLength = value; }
        public bool SendChunked { get => _httpWebRequest.SendChunked; set => _httpWebRequest.SendChunked = value; }
        public string UserAgent { get => _httpWebRequest.UserAgent; set => _httpWebRequest.UserAgent = value; }
        public int Timeout { get => _httpWebRequest.Timeout; set => _httpWebRequest.Timeout = value; }

        public void Abort()
        {
            _httpWebRequest.Abort();
        }

        public IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state)
        {
            return _httpWebRequest.BeginGetRequestStream(callback, state);
        }

        public IAsyncResult BeginGetResponse(AsyncCallback callback, object state)
        {
            return _httpWebRequest.BeginGetResponse(callback, state);
        }

        public Stream EndGetRequestStream(IAsyncResult asyncResult)
        {
            return _httpWebRequest.EndGetRequestStream(asyncResult);
        }

        public WebResponse EndGetResponse(IAsyncResult asyncResult)
        {
            return (_httpWebRequest).EndGetResponse(asyncResult);
        }
    }
}
