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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.VisualBasic.FileIO
{
    public class TextFieldParser : IDisposable
    {
        TextReader m_Reader;
        bool m_LeaveOpen;
        string[] m_CommentTokens = new string[0];
        string[] m_Delimiters;
        string m_ErrorLine = string.Empty;
        long m_ErrorLineNumber = -1;
        int[] m_FieldWidths;
        bool m_HasFieldsEnclosedInQuotes = true;
        long m_LineNumber = -1;
        FieldType m_TextFieldType;
        bool m_TrimWhiteSpace = true;
        Queue<string> m_PeekedLine = new Queue<string>();
        int m_MinFieldLength;
        bool disposedValue;

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
            m_Reader = reader;
            m_LeaveOpen = leaveOpen;
        }

        string[] GetDelimitedFields()
        {
            if (m_Delimiters == null || m_Delimiters.Length == 0)
                throw new InvalidOperationException("Unable to read delimited fields because Delimiters is Nothing or empty.");
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

            var currentIndex = 0;
            var inQuote = false;
            if (m_HasFieldsEnclosedInQuotes && line[currentIndex] == '"')
            {
                inQuote = true;
                ++startIndex;
            }

            currentIndex = startIndex;

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

                for (var i = 0; i <= m_Delimiters.Length - 1; i++)
                {
                    if (string.Compare(line, j, m_Delimiters[i], 0, m_Delimiters[i].Length) == 0)
                    {
                        nextIndex = j + m_Delimiters[i].Length;
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
            m_ErrorLineNumber = m_LineNumber;
            m_ErrorLine = line;
            throw new MalformedLineException("Line " + m_ErrorLineNumber + " cannot be parsed using the current Delimiters.", m_ErrorLineNumber);
        }

        void RaiseFieldWidthEx(string line)
        {
            m_ErrorLineNumber = m_LineNumber;
            m_ErrorLine = line;
            throw new MalformedLineException("Line " + m_ErrorLineNumber + " cannot be parsed using the current FieldWidths.", m_ErrorLineNumber);
        }

        string[] GetWidthFields()
        {
            if (m_FieldWidths == null || m_FieldWidths.Length == 0)
                throw new InvalidOperationException("Unable to read fixed width fields because FieldWidths is Nothing or empty.");

            var result = new string[m_FieldWidths.Length - 1 + 1];

            var line = GetNextLine();

            if (line.Length < m_MinFieldLength)
                RaiseFieldWidthEx(line);

            var startIndex = 0;
            for (var i = 0; i <= result.Length - 1; i++)
            {
                result[i] = !m_TrimWhiteSpace
                          ? line.Substring(startIndex, m_FieldWidths[i])
                          : line.Substring(startIndex, m_FieldWidths[i]).Trim();
                startIndex += m_FieldWidths[i];
            }

            return result;
        }

        bool IsCommentLine(string line)
        {
            if (m_CommentTokens == null)
                return false;

            foreach (var str in m_CommentTokens)
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
            m_PeekedLine.Count > 0
            ? m_PeekedLine.Dequeue()
            : GetNextRealLine();

        public void Close()
        {
            if (m_Reader != null && !m_LeaveOpen)
                m_Reader.Close();
            m_Reader = null;
        }

        ~TextFieldParser() => Dispose(false);

        public string PeekChars(int numberOfChars)
        {
            if (numberOfChars < 1)
                throw new ArgumentException("numberOfChars has to be a positive, non-zero number", nameof(numberOfChars));

            string theLine = null;
            if (m_PeekedLine.Count > 0)
            {
                var peekedLines = m_PeekedLine.ToArray();
                for (var i = 0; i <= m_PeekedLine.Count - 1; i++)
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
                    theLine = m_Reader.ReadLine();
                    m_PeekedLine.Enqueue(theLine);
                }
                while (theLine != null && IsCommentLine(theLine));
            }

            return theLine != null
                 ? (theLine.Length <= numberOfChars ? theLine : theLine.Substring(0, numberOfChars))
                 : null;
        }

        public string[] ReadFields()
        {
            switch (m_TextFieldType)
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
            if (m_PeekedLine.Count > 0)
                return m_PeekedLine.Dequeue();
            m_LineNumber = m_LineNumber == -1 ? 1 : m_LineNumber + 1;
            return m_Reader.ReadLine();
        }

        public string ReadToEnd() =>
            m_Reader.ReadToEnd();

        public void SetDelimiters(params string[] delimiters) =>
            Delimiters = delimiters;

        public void SetFieldWidths(params int[] fieldWidths) =>
            FieldWidths = fieldWidths;

        public string[] CommentTokens
        {
            get => m_CommentTokens;
            set => m_CommentTokens = value;
        }

        public string[] Delimiters
        {
            get => m_Delimiters;
            set => m_Delimiters = value;
        }

        public bool EndOfData => PeekChars(1) == null;

        public string ErrorLine => m_ErrorLine;

        public long ErrorLineNumber => m_ErrorLineNumber;

        public int[] FieldWidths
        {
            get => m_FieldWidths;
            set
            {
                m_FieldWidths = value;
                if (m_FieldWidths != null)
                {
                    m_MinFieldLength = 0;
                    for (var i = 0; i <= m_FieldWidths.Length - 1; i++)
                        m_MinFieldLength += value[i];
                }
            }
        }

        public bool HasFieldsEnclosedInQuotes
        {
            get => m_HasFieldsEnclosedInQuotes;
            set => m_HasFieldsEnclosedInQuotes = value;
        }

        public long LineNumber => m_LineNumber;

        public FieldType TextFieldType
        {
            get => m_TextFieldType;
            set => m_TextFieldType = value;
        }

        public bool TrimWhiteSpace
        {
            get => m_TrimWhiteSpace;
            set => m_TrimWhiteSpace = value;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
                Close();
            disposedValue = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
