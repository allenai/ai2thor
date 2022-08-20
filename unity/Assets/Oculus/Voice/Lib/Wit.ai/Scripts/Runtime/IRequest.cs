/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Net;
using System.IO;

namespace Facebook.WitAi
{
    public interface IRequest
    {
        WebHeaderCollection Headers { get; set; }
        string Method { get; set; }
        string ContentType { get; set; }
        long ContentLength { get; set; }
        bool SendChunked { get; set; }
        string UserAgent { get; set; }
        int Timeout { get; set; }

        IAsyncResult BeginGetRequestStream(AsyncCallback callback, object state);
        IAsyncResult BeginGetResponse(AsyncCallback callback, object state);
        /// <summary>
        /// Returns a Stream for writing data to the Internet resource.
        /// </summary>
        /// <param name="asyncResult"></param>
        /// <returns></returns>
        Stream EndGetRequestStream(IAsyncResult asyncResult);
        WebResponse EndGetResponse(IAsyncResult asyncResult);

        void Abort();
    }
}
