using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextEditor
{
    public class TextEditor
    {

        //current doc in focus
        private Document current_document = null;

        //Clipboard shared across all docs
        public string clip_board = "";

        //Map for storing docs
        private Dictionary<string, Document> docs_map = new Dictionary<string, Document>();

        /*This section is all just wrappers for test driver to call the implemented 
            instance methods on current document instance
        */
        #region instance method callers
        public string Append(string text)
        {
            return current_document.Append(text);
        }

        public void Move(int position)
        {
            current_document.Move(position);
        }

        public string Backspace()
        {
            return current_document.Backspace();
        }

        public void Copy() 
        {
            current_document.Copy();
        }

        public void Select(int leftPosition, int rightPosition) 
        {
            current_document.Select(leftPosition, rightPosition);
        }

        public string Paste()
        {
            return current_document.Paste();
        }

        public string Undo()
        {
            return current_document.Undo();
        }

        public string Redo()
        {
            return current_document.Redo();
        }

        /// <summary>
        /// Creates a document with given name and adds to map
        /// if its valid to create
        /// </summary>
        /// <param name="doc_name">Name of document</param>
        public void Create(string doc_name)
        {
            if (docs_map.ContainsKey(doc_name))
            {
                throw new ArgumentException($"Document {doc_name} already exists\n");
            }
            else
            {
                docs_map.Add(doc_name, new Document(doc_name, this));
            }
        }

        /// <summary>
        /// Switches to document with given name
        /// if it exists
        /// </summary>
        /// <param name="doc_name">Document to switch to</param>
        public void Switch(string doc_name)
        {
            if (!docs_map.ContainsKey(doc_name))
            {
                throw new ArgumentException($"Document {doc_name} doesnt exist\n");
            }
            else
            {
                this.current_document = docs_map[doc_name];
            }
        }

        #endregion

        /// <summary>
        /// Helper Class that stores various state info about a single document
        /// instance
        /// </summary>
        internal class Document
        {
            //Name of document
            public string document_name = "";

            //Current state of cursor, selected text, and contents
            private Text_Cursor_State state = null;

            //Queue and stack for undoing and redoing changes to document text and cursor state
            private Stack<Text_Cursor_State> undo_stack = new Stack<Text_Cursor_State>();
            private Queue<Text_Cursor_State> redo_queue = new Queue<Text_Cursor_State>();

            //Handle back to editor instance for access to shared clipboard
            TextEditor editor_instance = null;

            //Constructor
            public Document(string document_name, TextEditor editor_instance)
            {
                this.document_name = document_name;
                this.editor_instance = editor_instance;

                this.state = new Text_Cursor_State();
            }


            #region Modify_Commands
            /// <summary>
            /// Inserts a given string to current cursor position
            /// and moves cursor to end of that append string within document
            /// </summary>
            /// <param name="text">string to append</param>
            /// <returns>current document</returns>
            public string Append(string text)
            {
                //If nothing is selected we dont have to paste over it
                if (state.selected != null)
                {
                    state.contents = state.contents.Remove(state.selected.Item1, state.selected.Item2);
                    state.contents = state.contents.Insert(state.selected.Item1, text);

                    Move(state.selected.Item1 + text.Length);

                    state.selected = null;
                }
                //Append text at current cursor position if nothing selected
                else
                {
                    state.contents = state.contents.Insert(state.cursor_position, text);

                    Move(state.cursor_position + text.Length);

                }

                return state.contents;
            }
            /// <summary>
            /// Removes character behind cursor
            /// </summary>
            /// <returns>string with character before cursor removed</returns>
            public string Backspace()
            {
                //If cursor isnt at beginning remove character behind
                //We dont really have to worry about bounds as Move function handles that but just double check
                if (state.cursor_position > 0)
                {
                    state.contents = state.contents.Remove(state.cursor_position - 1, 1);

                    //Update Cursor Position
                    Move(state.cursor_position - 1);

                    return state.contents;
                }
                //If at front nothing to backspace
                else
                    return state.contents;
            }
            #endregion
            #region Cursor_Commands
            /// <summary>
            /// Selects a substring within the current document
            /// </summary>
            /// <param name="leftPosition">left index</param>
            /// <param name="rightPosition">right index</param>
            public void Select(int leftPosition, int rightPosition)
            {
                //Bounds checks
                if (leftPosition > rightPosition)
                    return;
                if (leftPosition < 0)
                    return;
                if (leftPosition > state.contents.Length)
                    return;
                if (rightPosition < 0)
                    return;
                if (rightPosition > state.contents.Length)
                    return;
                //If indexes are the same just move cursor to that position
                if (leftPosition == rightPosition)
                {
                    state.cursor_position = leftPosition;
                    //Since we are just moving cursor double check selected is null
                    state.selected = null;
                    return;
                }

                //Create tupe of selected start index and length
                state.selected = new Tuple<int, int>(leftPosition, rightPosition - leftPosition);


            }

            /// <summary>
            /// Moves a cursor with in the bounds of the current document
            /// </summary>
            /// <param name="position">index to move cursor to</param>
            public void Move(int position)
            {
                //Check given index is within the current document bounds
                if (0 <= position && position < state.contents.Length)
                {
                    state.cursor_position = position;
                }
                //if before start move to start
                else if (position < 0)
                    state.cursor_position = 0;
                //if past end of string move to last index
                else
                    state.cursor_position = state.contents.Length - 1;
            }

            #endregion
            /// <summary>
            /// Copies selected text if any into clipboard
            /// </summary>
            public void Copy()
            {
                //If something is selected copy it to the clipboard
                if (state.selected != null)
                    editor_instance.clip_board = state.contents.Substring(state.selected.Item1, state.selected.Item2);

                //If nothing is make sure selected set to null and leave current clipboard contents
                state.selected = null;
            }
            /// <summary>
            /// Pastes text from clipboard if any to current cursor positon
            /// </summary>
            public  string Paste()
            {
                //If clipboard contains something to paste pass to Append function to handle
                if (editor_instance.clip_board != "")
                {
                    return Append(editor_instance.clip_board);
                }

                return state.contents;
                //Not sure if clear clip board after pasting
                //clip_board = "";

            }

            /// <summary>
            /// Undoes previous modify command
            /// </summary>
            /// <returns>string with modification undone</returns>
            public string Undo()
            {
                //Check if there are modifications done to current document
                if (undo_stack.Count > 0)
                {
                    //Add undo to redo just incase we want to redo what we just undid
                    redo_queue.Enqueue(state);

                    //Set state to state on top of Undo stack 
                    state = undo_stack.Pop();
                    //Return document contents
                    return state.contents;
                }

                //Nothing to undo return document
                return state.contents;

            }

            /// <summary>
            /// Undoes the undo command
            /// </summary>
            /// <returns>string with modification added back</returns>
            public string Redo()
            {
                //If we want to undo this redo append state to stack before redoing
                undo_stack.Push(state);

                //If we have something we previously undid redo it
                if (redo_queue.Count > 0)
                {
                    //Remove state first in queue and set as current state
                    state = redo_queue.Dequeue();
                    return state.contents;
                }

                //Nothing to redo just return contents
                return state.contents;

            }

        }

        /// <summary>
        /// For use in document class stores state info of cursor and text
        /// </summary>
        internal class Text_Cursor_State
        {
            public int cursor_position = 0;

            //Tuple with start index and length of selected text
            public Tuple<int, int> selected = null;

            //doucment contents
            public string contents = "";

        }

    }


}
