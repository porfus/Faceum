// MtCnn_lite.cpp : Этот файл содержит функцию "main". Здесь начинается и заканчивается выполнение программы.
//

#include <iostream>
#include <opencv2\core\core.hpp>
#include <opencv2\highgui\highgui.hpp>
#include "mtcnn.h"



typedef struct _tagMTCNNResult {
	cv::Rect r;
	float score;
}MTCNNResult;
std::vector<MTCNNResult> mtcnnDetection(mtcnn& _find, cv::Mat image, int min_img_size)
{
	std::vector<MTCNNResult> m_results;

	//detect face by min_size(min_img_size)
	std::vector<struct Bbox> detBox = _find.findFace(image, min_img_size);

	for (std::vector<struct Bbox>::iterator it = detBox.begin(); it != detBox.end(); it++)
	{
		if ((*it).exist)
		{
			float det_score = (*it).score;
			if (det_score > 0.7) {
				rectangle(image, Point((*it).y1, (*it).x1), Point((*it).y2, (*it).x2), Scalar(0, 0, 255), 2, 8, 0);
				for (int num = 0; num < 5; num++)circle(image, Point((int) * (it->ppoint + num), (int) * (it->ppoint + num + 5)), 3, Scalar(0, 255, 255), -1);
			}

			cv::Rect r1(Point((*it).y1, (*it).x1), Point((*it).y2, (*it).x2));

			MTCNNResult result;
			result.r = r1;
			result.score = det_score;

			m_results.push_back(result);
		}
	}

	return m_results;
}

int main()
{
	
	cv::Mat image = cv::imread("img.jpg");
	cv::imwrite("test.jpg", image);
	mtcnn find;
	auto resus = find.findFace(image, 30);

	auto ffff = mtcnnDetection(find, image, 30);

    std::cout << "Hello World!\n";
}

// Запуск программы: CTRL+F5 или меню "Отладка" > "Запуск без отладки"
// Отладка программы: F5 или меню "Отладка" > "Запустить отладку"

// Советы по началу работы 
//   1. В окне обозревателя решений можно добавлять файлы и управлять ими.
//   2. В окне Team Explorer можно подключиться к системе управления версиями.
//   3. В окне "Выходные данные" можно просматривать выходные данные сборки и другие сообщения.
//   4. В окне "Список ошибок" можно просматривать ошибки.
//   5. Последовательно выберите пункты меню "Проект" > "Добавить новый элемент", чтобы создать файлы кода, или "Проект" > "Добавить существующий элемент", чтобы добавить в проект существующие файлы кода.
//   6. Чтобы снова открыть этот проект позже, выберите пункты меню "Файл" > "Открыть" > "Проект" и выберите SLN-файл.
