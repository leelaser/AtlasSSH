﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasSSH
{
    public class LineBuffer
    {
        public const string CrLf = "\r\n";

        public LineBuffer(Action<string> actionOnline = null)
        {
            if (actionOnline != null)
            {
                _actionsOnline.Add(actionOnline);
            }
        }

        /// <summary>
        /// The current text buffer
        /// </summary>
        string _text = "";

        private List<string> _seen = new List<string>();

        /// <summary>
        /// Return remaining text
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var alreadyDone = _seen.Count > 0 ? _seen.Aggregate((all, newLine) => $"{all}//{newLine}") : "";
            return alreadyDone + "//" + _text;
        }

        /// <summary>
        /// List of actions to perform when there is new line.
        /// </summary>
        private List<Action<string>> _actionsOnline = new List<Action<string>>();

        /// <summary>
        /// Add a new action to our list of actions.
        /// </summary>
        /// <param name="act"></param>
        public void AddAction (Action<string> act)
        {
            _actionsOnline.Add(act);
        }

        /// <summary>
        /// Add text. A line is delimited by CrLf.
        /// </summary>
        /// <param name="text">Less than, more than, or exactly one line of text to be added to the buffer</param>
        public void Add(string text)
        {
            _text += text;
            Flush();
        }

        /// <summary>
        /// Walk through our text, flushing all lines out.
        /// </summary>
        private void Flush()
        {
            while (true)
            {
                var lend = _text.IndexOf(CrLf);
                if (lend < 0)
                    break;

                var line = _text.Substring(0, lend);
                ActOnLine(line);
                _text = _text.Substring(lend + 2);
                _seen.Add(line);
            }
        }

        public void DumpRest()
        {
            Flush();
            if (_text.Length > 0)
            {
                ActOnLine(_text);
            }
        }

        List<string> stringsToSuppress = new List<string>();

        /// <summary>
        /// Any line containing these strings will not be printed out
        /// </summary>
        /// <param name="whenLineContains"></param>
        public void Suppress(string whenLineContains)
        {
            stringsToSuppress.Add(whenLineContains);
        }

        /// <summary>
        /// See if the text is in the current buffer
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool Match(string text)
        {
            return _text.Contains(text);
        }

        /// <summary>
        /// Dump out a line, safely.
        /// </summary>
        /// <param name="line"></param>
        private void ActOnLine(string line)
        {
            //Trace.WriteLine("ReturnedLine: " + line, "SSHConnection");

            if (!stringsToSuppress.Any(s => line.Contains(s)))
            {
                foreach (var a in _actionsOnline)
                {
                    a(line);
                }
            }
        }
    }
}
