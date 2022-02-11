using System;
using System.Collections.Generic;

namespace Tekly.Logging
{
    public class TkLogLevelsTree
    {
        internal class LevelNode
        {
            public string Value;
            public List<LevelNode> Children = new List<LevelNode>();
            public TkLogLevel? Level;
        }
        
        private LevelNode m_rootNode = new LevelNode();
        
        public void Initialize(List<LoggerLevelPair> levels)
        {
            m_rootNode = new LevelNode();

            TkLogLevel? minimum = null;
            
            foreach (var level in levels) {
                if (level.Logger == "*") {
                    minimum = level.Level;
                    continue;
                }
                
                AddLevel(level);
            }

            if (minimum.HasValue) {
                TkLogger.SetGlobalMinLogLevel(minimum.Value);
            }
        }

        public TkLogLevel GetLevel(string name)
        {
            TkLogLevel lastLevel = TkLogger.GlobalMinLogLevel;
            
            LevelNode lastNode = m_rootNode;
            var split = name.Split('.');

            foreach (var s in split) {
                if (TryGet(lastNode.Children, s, out var newNode)) {
                    if (newNode.Level.HasValue) {
                        lastLevel = newNode.Level.Value;
                    }
                    lastNode = newNode;
                } else {
                    break;
                }
            }

            return lastLevel;
        }

        private void AddLevel(LoggerLevelPair level)
        {
            var split = level.Logger.Split('.');

            LevelNode lastNode = m_rootNode;
            
            foreach (var s in split) {
                lastNode = GetOrCreate(lastNode.Children, s);
            }

            lastNode.Level = level.Level;
        }

        private LevelNode GetOrCreate(List<LevelNode> nodes, string name)
        {
            foreach (var node in nodes) {
                if (string.Equals(node.Value, name, StringComparison.OrdinalIgnoreCase)) {
                    return node;
                }
            }

            var newNode = new LevelNode {
                Value = name
            };
            
            nodes.Add(newNode);

            return newNode;
        }
        
        private bool TryGet(List<LevelNode> nodes, string name, out LevelNode targetNode)
        {
            foreach (var node in nodes) {
                if (string.Equals(node.Value, name, StringComparison.OrdinalIgnoreCase)) {
                    targetNode = node;
                    return true;
                }
            }

            targetNode = null;
            return false;
        }
    }
}