using EquipmentSimulator.Desktop.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace EquipmentSimulator.Desktop.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private string _equipmentId;
        private string _equipmentState;
        private string _connectionStatus;

        public MainViewModel()
        {
            EquipmentId = "EQ001";
            EquipmentState = "IDLE";
            ConnectionStatus = "Disconnected";

            ConnectCommand = new RelayCommand(Connect);
            DisconnectCommand = new RelayCommand(Disconnect);
        }

        public string EquipmentId
        {
            get { return _equipmentId; }
            set
            {
                if (_equipmentId == value)
                    return;

                _equipmentId = value;
                OnPropertyChanged();
            }
        }

        public string EquipmentState
        {
            get { return _equipmentState; }
            set
            {
                if (_equipmentState == value)
                    return;

                _equipmentState = value;
                OnPropertyChanged();
            }
        }

        public string ConnectionStatus
        {
            get { return _connectionStatus; }
            set
            {
                if (_connectionStatus == value)
                    return;

                _connectionStatus = value;
                OnPropertyChanged();
            }
        }
        public ICommand ConnectCommand { get; }

        public ICommand DisconnectCommand { get; }

        private void Connect()
        {
            ConnectionStatus = "Connected";
        }

        private void Disconnect()
        {
            ConnectionStatus = "Disconnected";
        }
    }
}