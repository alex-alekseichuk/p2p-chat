#ifndef __COMMON__LOGGER__H__
#define __COMMON__LOGGER__H__

#include <iostream>

namespace common
{
	class Logger
	{
	public:	
		Logger()
		{
		}

		inline void log(const char *str)
		{
			std::cerr << str << std::endl;
		}
		inline void debug(const char *str)
		{
			std::cerr << str << std::endl;
		}
	};

}

#endif

