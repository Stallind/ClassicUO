﻿#region license

//  Copyright (C) 2019 ClassicUO Development Community on Github
//
//	This project is an alternative client for the game Ultima Online.
//	The goal of this is to develop a lightweight client considering 
//	new technologies.  
//      
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using System;
using System.IO;

using ClassicUO.Configuration;
using ClassicUO.Utility;
using ClassicUO.Utility.Collections;
using ClassicUO.Utility.Logging;

namespace ClassicUO.Game.Managers
{
    internal class JournalManager
    {
        private StreamWriter _fileWriter;
        private bool _writerHasException;

        public Deque<JournalEntry> Entries { get; } = new Deque<JournalEntry>();

        public event EventHandler<JournalEntry> EntryAdded;

        public void Add(string text, ushort hue, string name, bool isunicode = true)
        {
            if (Entries.Count >= 100)
                Entries.RemoveFromFront();

            byte font = (byte) (isunicode ? 0 : 9);

            if (ProfileManager.Current != null && ProfileManager.Current.OverrideAllFonts)
            {
                font = ProfileManager.Current.ChatFont;
                isunicode = ProfileManager.Current.OverrideAllFontsIsUnicode;
            }

            var n = DateTime.Now;
            JournalEntry entry = new JournalEntry(text, font, hue, name, isunicode, n);
            Entries.AddToBack(entry);
            EntryAdded.Raise(entry);

            if (_fileWriter == null && !_writerHasException)
            {
                CreateWriter();
            }

            _fileWriter?.WriteLine($"[{n:g}]  {name}: {text}");
        }

        private void CreateWriter()
        {
            if (_fileWriter == null && ProfileManager.Current.SaveJournalToFile)
            {
                try
                {
                    string path = FileSystemHelper.CreateFolderIfNotExists(Path.Combine(CUOEnviroment.ExecutablePath, "Data"), "Client", "JournalLogs");
                    path = Path.Combine(path, $"{DateTime.Now:yyyy_MM_dd_HH_mm_ss}_journal.txt");

                    _fileWriter = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                    // we don't want to wast time.
                    _writerHasException = true;
                }
            }
        }

        public void CloseWriter()
        {
            _fileWriter?.Flush();
            _fileWriter?.Dispose();
            _fileWriter = null;
        }

        public void Clear()
        {
            Entries.Clear();
            CloseWriter();
        }
    }

    internal class JournalEntry
    {
        public readonly byte Font;
        public readonly ushort Hue;

        public readonly bool IsUnicode;
        public readonly string Name;
        public readonly string Text;
        public readonly DateTime Time;

        public JournalEntry(string text, byte font, ushort hue, string name, bool isunicode, DateTime time)
        {
            IsUnicode = isunicode;
            Font = font;
            Hue = hue;
            Name = name;
            Text = text;
            Time = time;
        }
    }
}