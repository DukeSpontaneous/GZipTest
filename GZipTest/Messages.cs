﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipTest
{
    class Messages
    {
        public const string CMD_HELP = @"Согласно постановке задания, для взаимодействия с утилитой введите команду в следующем формате:
GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла]

Для разархивации сегментированного архива укажите начальный сегмент, имеющий расширение `.gz.0`.

Предупреждение: утилита не предназначена для работы архивами, сегментированными другими архиваторами!
Также на данный момент программа устанавливает размер сегмента константно. Для того, чтобы изменить размер сегмента, программу нужно перекомпилировать.
";

        public const string CMD_WAITING = @"Начата обработка данных...";
        public const string CMD_SUCCESS = @"Команда успешно выполнена!";
    }
}
