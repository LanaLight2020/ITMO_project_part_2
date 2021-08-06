using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using System.IO;

// Дополнительные пространства имен
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections;

namespace Server_test
{
    public partial class Form1 : Form
    {
        TcpListener server = null;// Ссылка на сервер
        int port = 12000;
        String hostName = "127.0.0.1";// local
        IPAddress localAddr;
        string fileName;
        public Form1()
        {
            InitializeComponent();

        }

        private void start_button_Click(object sender, EventArgs e)
        {

            localAddr = IPAddress.Parse(hostName);// Конвертируем в другой формат

            //Выбираем файл Excel, откуда возьмем данные, Data.xlsx
            //Считываем данные из Excel
            //После подключения клиента передаем ему данные
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                rowCount.Text = (DateTime.Now).ToString();
                fileName = openFileDialog1.FileName;
                Thread thread = new Thread(ExecuteLoop);
                thread.IsBackground = true;
                thread.Start();
            }

        }

        private void ExecuteLoop()
        {
            // Параметры, которые определяют размер считываемых данных
            // Число строк в группе, котрые потом повторяются
            int groupCountRead = 50;
            // Число повторов
            int repeatNumbers = 60;
            // Общее число строк в исходном файле
            int generalRows = 1000;//38400;// 89200;// 178400;
            bool endReading = true;

            //Чтение из Excel
            //Создаём приложение
            Microsoft.Office.Interop.Excel.Application ObjExcel = new Microsoft.Office.Interop.Excel.Application();
            //Открываем книгу.    
            Microsoft.Office.Interop.Excel.Workbook ObjWorkBook = ObjExcel.Workbooks.Open(fileName);
            //Выбираем таблицу(лист).
            Microsoft.Office.Interop.Excel.Worksheet ObjWorkSheet;
            ObjWorkSheet = (Microsoft.Office.Interop.Excel.Worksheet)ObjWorkBook.Sheets[1];

            // Создаем поток для записи в текстовый файл - использовалось при отладке
            //StreamWriter sv = new StreamWriter(@"D:\Sveta\Programms\Out_file.txt");
            int count = 0;
            string[] rowKept = new string[groupCountRead];
            int groupNumber = 1; //Номер копируемой группы строк
            int deep = 0; // новый вводимый в данные параметр - глубина - просто число в данных
                          //максимальное число groupNumber
            int generalGroups = generalRows - groupCountRead + 1;

            server = new TcpListener(localAddr, port);// Создаем сервер-слушатель
            server.Start();// Запускаем сервер
            String data;

            // Бесконечный цикл прослушивания клиентов
            while (endReading)
            {

                if (!server.Pending())// Очередь запросов пуста
                    continue;
                TcpClient client = server.AcceptTcpClient();// Текущий клиент
                                                            // Сами задаем размеры буферов обмена (Необязательно!)
                                                            // По умолчанию оба буфера установлены размером по 8192 байта
                                                            // client.SendBufferSize = client.ReceiveBufferSize = 1024;

                // Подключаем NetworkStream и погружаем для удобства в оболочки
                NetworkStream streamIn = client.GetStream();
                NetworkStream streamOut = client.GetStream();
                StreamReader readerStream = new StreamReader(streamIn);
                StreamWriter writerStream = new StreamWriter(streamOut);

                // Читаем запрос
                data = readerStream.ReadLine();
                if (Int32.Parse(data) == 4) // если от клиента пришло "4", то отправляем данные
                {
                    while (groupNumber <= generalGroups)
                    {
                        for (int i = 1 + groupNumber; i <= groupCountRead + groupNumber; i++)
                        {

                            string rowText = "";
                            for (int j = 2; j <= 51; j++)
                            {
                                rowText = rowText + ObjWorkSheet.Cells[i + 60000, j].Value.ToString() + " ";
                            }
                            // Записываем строки в массив 
                            rowKept[i - groupNumber - 1] = rowText;

                        }

                        // Записываем строки, сохраненные в массиве, в поток для передачи клиенту repeatNumbers раз
                        for (int repeat = 1; repeat <= repeatNumbers; repeat++)
                        {
                            for (int i = 1; i <= groupCountRead; i++)
                            {
                                count++;
                                //формируем итоговую строку для передачи клиенту
                                string rowText = "85 " + count.ToString() + " " + rowKept[i - 1] + deep.ToString() + " 0 0 0 0 0 0 0 0 170";
                                //Записываем строку в файл - при отладке использовалось
                                // sv.WriteLine(rowText);
                                // Отправляем данные клиенту
                                writerStream.WriteLine(rowText);
                                writerStream.Flush();
                                /// устанавливаем значения глубины для последующей записи в данные
                                switch (count)
                                {
                                    case 10000: deep = -20; break;
                                    case 12000: deep = -40; break;
                                    case 14000: deep = -60; break;
                                    case 16000: deep = -80; break;
                                    case 18000: deep = -100; break;
                                    case 20000: deep = 120; break;
                                    case 24000: deep = 100; break;
                                    case 26000: deep = 80; break;
                                    case 28000: deep = 60; break;
                                    case 30000: deep = 40; break;
                                    case 32000: deep = 20; break;
                                    default: break;
                                }
                            }
                        }

                        groupNumber = groupNumber + groupCountRead;
                        // Выводим в форме номер записанной строки
                        rowCount.Text = count.ToString();
                        //это чтобы форма прорисовывалась (не подвисала)...
                        System.Windows.Forms.Application.DoEvents();
                    }
                    endReading = false;
                }


                //Удаляем приложение (выходим из экселя) - а то будет висеть в процессах!
                ObjExcel.Quit();
                // Закрываем соединение и потоки, порядок неважен
                client.Close();
                readerStream.Close();
                writerStream.Close();
                // Останавливаем сервер
                server.Stop();
                //sv.Close();
                MessageBox.Show("Работа сервера закончена!");
            }



        }
    }
}
