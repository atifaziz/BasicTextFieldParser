//
// Authors (of original Visual Basic version):
//   Rolf Bjarne Kvinge <RKvinge@novell.com>
//
// Copyright (C) 2018 Atif Aziz
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

namespace BasicTextFieldParser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;

    #if BASIC_TEXT_FILE_PARSER_LIB
    public partial class TextFieldParser {}
    public partial class MalformedLineException {}
    public // ...
    #endif
    enum FieldType
    {
        Delimited,
        FixedWidth,
    }

    partial class TextFieldParser : IDisposable
    {
        TextReader _reader;
        readonly bool _leaveOpen;
        int[] _fieldWidths;
        readonly Queue<string> _peekedLine = new Queue<string>();
        int _minFieldLength;
        bool _disposedValue;

        public TextFieldParser(Stream stream) :
            this(new StreamReader(stream)) {}

        public TextFieldParser(TextReader reader) :
            this(reader, false) {}

        public TextFieldParser(string path) :
            this(new StreamReader(path), false) {}

        public TextFieldParser(Stream stream, Encoding defaultEncoding) :
            this(new StreamReader(stream, defaultEncoding)) {}

        public TextFieldParser(string path, Encoding defaultEncoding) :
            this(new StreamReader(path, defaultEncoding)) {}

        public TextFieldParser(Stream stream, Encoding defaultEncoding, bool detectEncoding) :
            this(new StreamReader(stream, defaultEncoding, detectEncoding)) {}

        public TextFieldParser(string path, Encoding defaultEncoding, bool detectEncoding) :
            this(new StreamReader(path, defaultEncoding, detectEncoding)) {}

        public TextFieldParser(Stream stream, Encoding defaultEncoding, bool detectEncoding, bool leaveOpen) :
            this(new StreamReader(stream, defaultEncoding, detectEncoding), leaveOpen) {}

        TextFieldParser(TextReader reader, bool leaveOpen)
        {
            _reader = reader;
            _leaveOpen = leaveOpen;
        }

        string[] GetDelimitedFields()
        {
            if (Delimiters == null || Delimiters.Length == 0)
                throw new InvalidOperationException("Unable to read delimited fields because " + nameof(Delimiters) + " is Nothing or empty.");
            var result = new List<string>();

            var line = GetNextLine();

            if (line == null)
                return null;

            var startIndex = 0;
            for (var nextIndex = 0; nextIndex < line.Length; startIndex = nextIndex)
                result.Add(GetNextField(line, startIndex, ref nextIndex));

            return result.ToArray();
        }

        string GetNextField(string line, int startIndex, ref int nextIndex)
        {
            if (nextIndex == int.MinValue)
            {
                nextIndex = int.MaxValue;
                return string.Empty;
            }

            var currentIndex = startIndex;
            var inQuote = false;
            if (HasFieldsEnclosedInQuotes && line[currentIndex] == '"')
            {
                inQuote = true;
                ++startIndex;
            }

            var mustMatch = false;
            for (var j = startIndex; j <= line.Length - 1; j++)
            {
                if (inQuote)
                {
                    if (line[j] == '"')
                    {
                        inQuote = false;
                        mustMatch = true;
                    }

                    continue;
                }

                var delimiters = Delimiters;
                for (var i = 0; i <= delimiters.Length - 1; i++)
                {
                    if (string.Compare(line, j, delimiters[i], 0, delimiters[i].Length) == 0)
                    {
                        nextIndex = j + delimiters[i].Length;
                        if (nextIndex == line.Length)
                            nextIndex = int.MinValue;
                        return mustMatch
                             ? line.Substring(startIndex, j - startIndex - 1)
                             : line.Substring(startIndex, j - startIndex);
                    }
                }

                if (mustMatch)
                    RaiseDelimiterEx(line);
            }

            if (inQuote)
                RaiseDelimiterEx(line);

            nextIndex = line.Length;

            return mustMatch
                 ? line.Substring(startIndex, nextIndex - startIndex - 1)
                 : line.Substring(startIndex);
        }

        void RaiseDelimiterEx(string line)
        {
            ErrorLineNumber = LineNumber;
            ErrorLine = line;
            throw new MalformedLineException($"Line {ErrorLineNumber} cannot be parsed using the current {nameof(Delimiters)}.", ErrorLineNumber);
        }

        void RaiseFieldWidthEx(string line)
        {
            ErrorLineNumber = LineNumber;
            ErrorLine = line;
            throw new MalformedLineException($"Line {ErrorLineNumber} cannot be parsed using the current {nameof(FieldWidths)}.", ErrorLineNumber);
        }

        string[] GetWidthFields()
        {
            if (_fieldWidths == null || _fieldWidths.Length == 0)
                throw new InvalidOperationException("Unable to read fixed width fields because " + nameof(FieldWidths) + " is Nothing or empty.");

            var result = new string[_fieldWidths.Length - 1 + 1];

            var line = GetNextLine();

            if (line.Length < _minFieldLength)
                RaiseFieldWidthEx(line);

            var startIndex = 0;
            var trimWhiteSpace = TrimWhiteSpace;
            for (var i = 0; i <= result.Length - 1; i++)
            {
                result[i] = !trimWhiteSpace
                          ? line.Substring(startIndex, _fieldWidths[i])
                          : line.Substring(startIndex, _fieldWidths[i]).Trim();
                startIndex += _fieldWidths[i];
            }

            return result;
        }

        bool IsCommentLine(string line)
        {
            if (CommentTokens == null)
                return false;

            foreach (var str in CommentTokens)
            {
                if (line.StartsWith(str))
                    return true;
            }

            return false;
        }

        string GetNextRealLine()
        {
            string line;
            do { line = ReadLine(); } while (line != null && IsCommentLine(line));
            return line;
        }

        string GetNextLine() =>
            _peekedLine.Count > 0
            ? _peekedLine.Dequeue()
            : GetNextRealLine();

        public void Close()
        {
            if (_reader != null && !_leaveOpen)
                _reader.Close();
            _reader = null;
        }

        ~TextFieldParser() => Dispose(false);

        public string PeekChars(int numberOfChars)
        {
            if (numberOfChars < 1)
                throw new ArgumentException(nameof(numberOfChars) + " has to be a positive, non-zero number", nameof(numberOfChars));

            string theLine = null;
            if (_peekedLine.Count > 0)
            {
                var peekedLines = _peekedLine.ToArray();
                for (var i = 0; i <= _peekedLine.Count - 1; i++)
                {
                    if (!IsCommentLine(peekedLines[i]))
                    {
                        theLine = peekedLines[i];
                        break;
                    }
                }
            }

            if (theLine == null)
            {
                do
                {
                    theLine = _reader.ReadLine();
                    _peekedLine.Enqueue(theLine);
                }
                while (theLine != null && IsCommentLine(theLine));
            }

            return theLine != null
                 ? (theLine.Length <= numberOfChars ? theLine : theLine.Substring(0, numberOfChars))
                 : null;
        }

        public string[] ReadFields()
        {
            switch (TextFieldType)
            {
                case FieldType.Delimited:
                    return GetDelimitedFields();
                case FieldType.FixedWidth:
                    return GetWidthFields();
                default:
                    return GetDelimitedFields();
            }
        }

        public string ReadLine()
        {
            if (_peekedLine.Count > 0)
                return _peekedLine.Dequeue();
            LineNumber = LineNumber == -1 ? 1 : LineNumber + 1;
            return _reader.ReadLine();
        }

        public string ReadToEnd() =>
            _reader.ReadToEnd();

        public bool EndOfData => PeekChars(1) == null;

        public void SetDelimiters(params string[] delimiters) =>
            Delimiters = delimiters;

        public void SetFieldWidths(params int[] fieldWidths) =>
            FieldWidths = fieldWidths;

        public string[]  CommentTokens             { get; set; }         = new string[0];
        public string[]  Delimiters                { get; set; }
        public string    ErrorLine                 { get; private set; } = string.Empty;
        public long      ErrorLineNumber           { get; private set; } = -1;
        public bool      HasFieldsEnclosedInQuotes { get; set; }         = true;
        public long      LineNumber                { get; private set; } = -1;
        public FieldType TextFieldType             { get; set; }         = FieldType.Delimited;
        public bool      TrimWhiteSpace            { get; set; }         = true;

        public int[] FieldWidths
        {
            get => _fieldWidths;
            set
            {
                _fieldWidths = value;
                if (_fieldWidths != null)
                {
                    _minFieldLength = 0;
                    for (var i = 0; i <= _fieldWidths.Length - 1; i++)
                        _minFieldLength += value[i];
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
                Close();
            _disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    [Serializable]
    partial class MalformedLineException : Exception
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
            const string typeName = nameof(BasicTextFieldParser) + "." + nameof(MalformedLineException);
            var msg = typeName + ": ";
            msg += !_anyMessage ? "Exception of type '" + typeName + "' was thrown." : Message;
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
