using System;
using System.Text;
using UnityEngine;

namespace Utilities
{
    public class StringUtilities : MonoBehaviour
    {
        static readonly StringBuilder stringBuilder = new();

        public static string EnumToSpacedString(Enum enumValue)
        {
            stringBuilder.Clear();
            string enumString = enumValue.ToString();

            stringBuilder.Append(enumString[0]); // Append the first character
            for (int i = 1; i < enumString.Length; i++)
            {
                char currentChar = enumString[i];

                // Insert a space before a capital letter that follows a lowercase letter
                if (char.IsUpper(currentChar) && char.IsLower(enumString[i - 1]))
                    stringBuilder.Append(' ');

                stringBuilder.Append(currentChar);
            }

            return stringBuilder.ToString();
        }

        public static string SplitTextIntoParagraphs(string originalText, int maxCharsPerLine)
        {
            stringBuilder.Clear();
            int currentLineLength = 0;
            bool insideTag = false;

            foreach (char c in originalText)
            {
                // Check if we are entering or exiting a tag
                if (c == '<') insideTag = true;
                else if (c == '>') insideTag = false;

                // Only process non-tag characters
                if (!insideTag)
                {
                    if (c == ' ' && currentLineLength >= maxCharsPerLine)
                    {
                        // New line at space if line length exceeded
                        stringBuilder.AppendLine();
                        currentLineLength = 0;
                    }
                    else
                    {
                        // Add character to line
                        stringBuilder.Append(c);
                        currentLineLength++;
                    }
                }
                else
                {
                    // Always add tag characters
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString();
        }


        /*public static string SplitTextIntoParagraphs(string originalText, int maxCharsPerLine)
        {
            stringBuilder.Clear();
            int currentLineLength = 0;

            foreach (string word in originalText.Split(' '))
            {
                if (currentLineLength + word.Length + 1 <= maxCharsPerLine)
                {
                    if (currentLineLength > 0)
                    {
                        stringBuilder.Append(' ');
                        currentLineLength++;
                    }

                    stringBuilder.Append(word);
                    currentLineLength += word.Length;
                }
                else
                {
                    stringBuilder.AppendLine();
                    stringBuilder.Append(word);
                    currentLineLength = word.Length;
                }
            }

            return stringBuilder.ToString();
        }*/
    }
}
