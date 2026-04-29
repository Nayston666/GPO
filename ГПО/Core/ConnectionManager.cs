using System;
using System.Collections.Generic;
using System.Linq;
using MathApp.Models;

namespace MathApp.Core
{
    public class ConnectionManager
    {
        private List<Connection> _connections;

        public ConnectionManager(List<Connection> connections)
        {
            _connections = connections;
        }

        /// <summary>
        /// Создает новое соединение между блоками
        /// </summary>
        public bool CreateConnection(ConnectionPoint source, ConnectionPoint target)
        {
            // Нельзя соединять блок сам с собой
            if (source.ToolId == target.ToolId)
                return false;

            if (target.InputType.HasValue)
            {
                _connections.RemoveAll(c =>
                    c.TargetToolId == target.ToolId &&
                    c.TargetInput == target.InputType.Value);  
            }
            else
            {
                // Для портов подсистемы
                _connections.RemoveAll(c =>
                    c.TargetToolId == target.ToolId &&
                    c.TargetPortIndex == target.PortIndex);
            }

            // Создаем новое
            var newConn = new Connection
            {
                Id = Guid.NewGuid(),
                SourceToolId = source.ToolId,
                TargetToolId = target.ToolId,
                TargetInput = target.InputType ?? InputType.A,  // Используем переданный тип входа
                TargetPortIndex = target.PortIndex,
                SourcePortIndex = source.PortIndex
            };

            _connections.Add(newConn);

            System.Diagnostics.Debug.WriteLine($"Created connection: Source={source.ToolId}, Target={target.ToolId}, Input={newConn.TargetInput}, PortIndex={target.PortIndex}");

            return true;
        }


        /// <summary>
        /// Удаляет все соединения, связанные с блоком
        /// </summary>
        public void RemoveConnectionsForTool(Guid toolId)
        {
            _connections.RemoveAll(c =>
                c.SourceToolId == toolId || c.TargetToolId == toolId);
        }

        /// <summary>
        /// Удаляет все входящие соединения для блока
        /// </summary>
        public void RemoveIncomingConnections(Guid toolId)
        {
            _connections.RemoveAll(c => c.TargetToolId == toolId);
        }
    }
}