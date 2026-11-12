
#include <iostream>
#include <fstream>

#include "expatcc.h"

class Parser : public expat::Expat
{
	private:
		virtual void startElement(const char* name, const char** atts);
		virtual void endElement(const char* name);
		virtual void charData(const char *s, int len);
};

void Parser::startElement(const char* name, const char** atts)
{
	for (int i = 0; i < getDepth(); ++i)
		std::cout << "\t";
	std::cout << "start tag: " << name;
	for (int i = 0; atts[i]; i += 2)
		std::cout << " " << atts[i] << "='" << atts[i + 1] << "'";
	std::cout << std::endl;
}

void Parser::endElement(const char* name)
{
	for (int i = 0; i < getDepth(); ++i)
		std::cout << "\t";
	std::cout << "end tag: " << name << std::endl;
}

void Parser::charData(const char *s, int len)
{
	for (int i = 0; i < getDepth(); ++i)
		std::cout << "\t";
	std::cout.write(s, len);
	std::cout << std::endl;
}


int main(int argc, char ** argv)
{
	Parser parser;

	std::ifstream inFile;
	std::string str;
	inFile.open (argv[1]);
	while (inFile.good())
	{
		getline (inFile, str);
		//std::cout << str;
		parser.parse(str.c_str(), str.length(), 0);
	}
	parser.parse(NULL, 0, 1);
	inFile.close();
	inFile.clear();

	std::cout << "done." << std::endl;
	return 0;
}

