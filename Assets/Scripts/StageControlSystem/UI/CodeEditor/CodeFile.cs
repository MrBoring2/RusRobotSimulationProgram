namespace Assets.UI.CodeEditor
{
    /// <summary>
    /// Тип файла кода
    /// </summary>
    public enum CodeFileType
    {
        PLC,      // Программа ПЛК
        Robot     // Программа робота
    }
    
    /// <summary>
    /// Информация о файле в редакторе
    /// </summary>
    public class CodeFile
    {
        public string Content { get; set; }
        public CodeFileType Type { get; set; }
        public string DisplayName { get; set; }
        public string RobotId { get; set; }
        public string RobotName { get; set; }

        public CodeFile(string content, CodeFileType type, string displayName, string robotId = null, string robotName = null)
        {
            Content = content;
            Type = type;
            DisplayName = displayName;
            RobotId = robotId;
            RobotName = robotName;
        }
    }
}