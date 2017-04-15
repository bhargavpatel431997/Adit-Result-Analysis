function getCsv() {
    var content = [['EnrollmentNo', 'StudentName', 'DepartmentId(IT,CP,ME02,ME08,FT,CIVIL,AE,EEE,ECE)', 'Semester number Last Result was came', 'Result Declared Year'], ['=TEXT("0",140010116024)', 'bhargavpatel', 'IT', '6', '2017']];

    var finalVal = '';

    for (var i = 0; i < content.length; i++) {
        var value = content[i];

        for (var j = 0; j < value.length; j++) {
            var innerValue = value[j] === null ? '' : value[j].toString();
            var result = innerValue.replace(/"/g, '""');
            if (result.search(/("|,|\n)/g) >= 0)
                result = '"' + result + '"';
            if (j > 0)
                finalVal += ',';
            finalVal += result;
        }

        finalVal += '\n';
    }

    console.log(finalVal);
    var download = document.getElementById('download');
    download.setAttribute('href', 'data:text/csv;charset=utf-8,' + encodeURIComponent(finalVal));
    download.setAttribute('download', 'First_Time_Entery_Of_Student.csv');
}

