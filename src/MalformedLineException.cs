//
// Authors:
//   Rolf Bjarne Kvinge (RKvinge@novell.com>
//
// Copyright (C) 2007 Novell (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Runtime.Serialization;

namespace Microsoft.VisualBasic.FileIO
{
    [Serializable]
    public class MalformedLineException : Exception
    {
        readonly bool _anyMessage;

        public MalformedLineException()
        {
        }

        public MalformedLineException(string message) :
            base(message)
        {
            _anyMessage = !string.IsNullOrEmpty(message);
        }

        protected MalformedLineException(SerializationInfo info, StreamingContext context) :
            base(info, context)
        {
            if (info == null)
                return;
            LineNumber = info.GetInt64(nameof(LineNumber));
        }

        public MalformedLineException(string message, Exception innerException) :
            base(message, innerException)
        {
            _anyMessage = !string.IsNullOrEmpty(message);
        }

        public MalformedLineException(string message, long lineNumber) :
            base(message)
        {
            LineNumber = lineNumber;
            _anyMessage = !string.IsNullOrEmpty(message);
        }

        public MalformedLineException(string message, long lineNumber, Exception innerException) :
            base(message, innerException)
        {
            LineNumber = lineNumber;
            _anyMessage = !string.IsNullOrEmpty(message);
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            if (info == null)
                return;
            info.AddValue("LineNumber", LineNumber);
        }

        public override string ToString()
        {
            var msg = "Microsoft.VisualBasic.FileIO.MalformedLineException: ";
            msg += !_anyMessage ? "Exception of type 'Microsoft.VisualBasic.FileIO.MalformedLineException' was thrown." : Message;
            if (InnerException != null)
            {
                msg += " ---> " + InnerException + Environment.NewLine;
                msg += InnerException.StackTrace + "   --- End of inner exception stack trace ---";
            }
            return msg + " Line Number:" + LineNumber;
        }

        public long LineNumber { get; set; }
    }
}
