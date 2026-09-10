using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AptekaIS
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        //Объявление объекта доступа к БД
        public static Models.AptekaISDbEntities DBApteka { get; set; }

        //Объявление объявление объекта-текущий вошедший пользователь
        public static Models.Users currentUser;
    }
}
