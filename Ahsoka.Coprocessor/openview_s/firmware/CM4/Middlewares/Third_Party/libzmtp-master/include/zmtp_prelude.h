/*  =========================================================================
    zmtp_prelude.h - ZMTP environment

    Copyright (c) contributors as noted in the AUTHORS file.
    This file is part of libzmtp, the C ZMTP stack.
    
    NOTE: this file is a subset of czmq_prelude.h, identical except for
    the last section that ties into libzmq. If you modify this file please
    consider backporting your patches to czmq_prelude.h.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
    =========================================================================
*/

#ifndef __ZMTP_PRELUDE_H_INCLUDED__
#define __ZMTP_PRELUDE_H_INCLUDED__

//- Establish the compiler and computer system ------------------------------
/*
 *  Defines zero or more of these symbols, for use in any non-portable
 *  code:
 *
 *  __WINDOWS__         Microsoft C/C++ with Windows calls
 *  __MSDOS__           System is MS-DOS (set if __WINDOWS__ set)
 *  __VMS__             System is VAX/VMS or Alpha/OpenVMS
 *  __UNIX__            System is UNIX
 *  __OS2__             System is OS/2
 *
 *  __IS_32BIT__        OS/compiler is 32 bits
 *  __IS_64BIT__        OS/compiler is 64 bits
 *
 *  When __UNIX__ is defined, we also define exactly one of these:
 *
 *  __UTYPE_AUX         Apple AUX
 *  __UTYPE_BEOS        BeOS
 *  __UTYPE_BSDOS       BSD/OS
 *  __UTYPE_DECALPHA    Digital UNIX (Alpha)
 *  __UTYPE_IBMAIX      IBM RS/6000 AIX
 *  __UTYPE_FREEBSD     FreeBSD
 *  __UTYPE_HPUX        HP/UX
 *  __UTYPE_ANDROID     Android
 *  __UTYPE_LINUX       Linux
 *  __UTYPE_MIPS        MIPS (BSD 4.3/System V mixture)
 *  __UTYPE_NETBSD      NetBSD
 *  __UTYPE_NEXT        NeXT
 *  __UTYPE_OPENBSD     OpenBSD
 *  __UTYPE_OSX         Apple Macintosh OS X
 *  __UTYPE_QNX         QNX
 *  __UTYPE_IRIX        Silicon Graphics IRIX
 *  __UTYPE_SINIX       SINIX-N (Siemens-Nixdorf Unix)
 *  __UTYPE_SUNOS       SunOS
 *  __UTYPE_SUNSOLARIS  Sun Solaris
 *  __UTYPE_UNIXWARE    SCO UnixWare
 *                      ... these are the ones I know about so far.
 *  __UTYPE_GENERIC     Any other UNIX
 *
 *  When __VMS__ is defined, we may define one or more of these:
 *
 *  __VMS_XOPEN         Supports XOPEN functions
 */

#if (defined (__64BIT__) || defined (__x86_64__))
#    define __IS_64BIT__                //  May have 64-bit OS/compiler
#else
#    define __IS_32BIT__                //  Else assume 32-bit OS/compiler
#endif

#if (defined WIN32 || defined _WIN32)
#   undef __WINDOWS__
#   define __WINDOWS__
#   undef __MSDOS__
#   define __MSDOS__
#endif

#if (defined WINDOWS || defined _WINDOWS || defined __WINDOWS__)
#   undef __WINDOWS__
#   define __WINDOWS__
#   undef __MSDOS__
#   define __MSDOS__
//  Stop cheeky warnings about "deprecated" functions like fopen
#   if _MSC_VER >= 1500
#       define _CRT_SECURE_NO_DEPRECATE
#       pragma warning(disable: 4996)
#   endif
#endif

//  MSDOS               Microsoft C
//  _MSC_VER            Microsoft C
#if (defined (MSDOS) || defined (_MSC_VER))
#   undef __MSDOS__
#   define __MSDOS__
#   if (defined (_DEBUG) && !defined (DEBUG))
#       define DEBUG
#   endif
#endif

#if (defined (__EMX__) && defined (__i386__))
#   undef __OS2__
#   define __OS2__
#endif

//  VMS                 VAX C (VAX/VMS)
//  __VMS               Dec C (Alpha/OpenVMS)
//  __vax__             gcc
#if (defined (VMS) || defined (__VMS) || defined (__vax__))
#   undef __VMS__
#   define __VMS__
#   if (__VMS_VER >= 70000000)
#       define __VMS_XOPEN
#   endif
#endif

//  Try to define a __UTYPE_xxx symbol...
//  unix                SunOS at least
//  __unix__            gcc
//  _POSIX_SOURCE is various UNIX systems, maybe also VAX/VMS
#if 0
#if (defined (unix) || defined (__unix__) || defined (_POSIX_SOURCE))
#   if (!defined (__VMS__))
#       undef __UNIX__
#       define __UNIX__
#       if (defined (__alpha))          //  Digital UNIX is 64-bit
#           undef  __IS_32BIT__
#           define __IS_64BIT__
#           define __UTYPE_DECALPHA
#       endif
#   endif
#endif
#endif


#if (defined (_AUX))
#   define __UTYPE_AUX
#   define __UNIX__
#elif (defined (__BEOS__))
#   define __UTYPE_BEOS
#   define __UNIX__
#elif (defined (__hpux))
#   define __UTYPE_HPUX
#   define __UNIX__
#   define _INCLUDE_HPUX_SOURCE
#   define _INCLUDE_XOPEN_SOURCE
#   define _INCLUDE_POSIX_SOURCE
#elif (defined (_AIX) || defined (AIX))
#   define __UTYPE_IBMAIX
#   define __UNIX__
#elif (defined (BSD) || defined (bsd))
#   define __UTYPE_BSDOS
#   define __UNIX__
#elif (defined (APPLE) || defined (__APPLE__))
#   define __UTYPE_GENERIC
#   define __UNIX__
#elif (defined (__ANDROID__))
#   define __UTYPE_ANDROID
#   define __UNIX__
#elif (defined (LINUX) || defined (linux))
#   define __UTYPE_LINUX
#   define __UNIX__
#   ifndef __NO_CTYPE
#   define __NO_CTYPE                   //  Suppress warnings on tolower()
#   endif
#elif (defined (Mips))
#   define __UTYPE_MIPS
#   define __UNIX__
#elif (defined (FreeBSD) || defined (__FreeBSD__))
#   define __UTYPE_FREEBSD
#   define __UNIX__
#elif (defined (NetBSD) || defined (__NetBSD__))
#   define __UTYPE_NETBSD
#   define __UNIX__
#elif (defined (OpenBSD) || defined (__OpenBSD__))
#   define __UTYPE_OPENBSD
#   define __UNIX__
#elif (defined (__APPLE__))
#   define __UTYPE_OSX
#   define __UNIX__
#elif (defined (NeXT))
#   define __UTYPE_NEXT
#   define __UNIX__
#elif (defined (__QNX__))
#   define __UTYPE_QNX
#   define __UNIX__
#elif (defined (sgi))
#   define __UTYPE_IRIX
#   define __UNIX__
#elif (defined (sinix))
#   define __UTYPE_SINIX
#   define __UNIX__
#elif (defined (SOLARIS) || defined (__SRV4))
#   define __UTYPE_SUNSOLARIS
#   define __UNIX__
#elif (defined (SUNOS) || defined (SUN) || defined (sun))
#   define __UTYPE_SUNOS
#   define __UNIX__
#elif (defined (__USLC__) || defined (UnixWare))
#   define __UTYPE_UNIXWARE
#   define __UNIX__
#elif (defined (__CYGWIN__))
#   define __UTYPE_CYGWIN
#   define __UNIX__
#elif (defined (__UNIX__))
#   define __UTYPE_GENERIC
#endif

//- Standard ANSI include files ---------------------------------------------

#include <ctype.h>
#include <limits.h>
#include <stdarg.h>
#include <stdio.h>
#include <stdlib.h>
#include <stddef.h>
#include <string.h>
#include <time.h>
#include <errno.h>
#include <float.h>
#include <math.h>
#include <signal.h>
#include <setjmp.h>
#include <assert.h>

//- System-specific include files -------------------------------------------

#if (defined (__MSDOS__))
#   if (defined (__WINDOWS__))
#       if (_WIN32_WINNT < 0x0501)
#           undef _WIN32_WINNT
#           define _WIN32_WINNT 0x0501
#       endif
#       if (!defined (FD_SETSIZE))
#           define FD_SETSIZE 1024      //  Max. filehandles/sockets
#       endif
#       include <direct.h>
#       include <winsock2.h>
#       include <windows.h>
#       include <process.h>
#       include <ws2tcpip.h>            //  For getnameinfo ()
#       include <iphlpapi.h>            //  For GetAdaptersAddresses ()
#   endif
#   include <malloc.h>
#   include <dos.h>
#   include <io.h>
#   include <fcntl.h>
#   include <sys/types.h>
#   include <sys/stat.h>
#   include <sys/utime.h>
#   include <share.h>
#endif

#if (defined (__UNIX__))
#   include <fcntl.h>
#   include <netdb.h>
#   include <unistd.h>
#   include <pthread.h>
#   include <dirent.h>
#   include <pwd.h>
#   include <grp.h>
#   include <utime.h>
#   include <inttypes.h>
#   include <sys/types.h>
#   include <sys/param.h>
#   include <sys/socket.h>
#   include <sys/time.h>
#   include <sys/stat.h>
#   include <sys/ioctl.h>
#   include <sys/file.h>
#   include <sys/wait.h>
#   include <sys/un.h>
#   include <sys/uio.h>             //  Let ZMTP build with libzmq/3.x
#   include <netinet/in.h>          //  Must come before arpa/inet.h
#   if (!defined (__UTYPE_ANDROID)) && (!defined (__UTYPE_IBMAIX)) \
    && (!defined (__UTYPE_HPUX))
#       include <ifaddrs.h>
#   endif
#   if defined (__UTYPE_SUNSOLARIS) || defined (__UTYPE_SUNOS)
#       include <sys/sockio.h>
#   endif
#   if (!defined (__UTYPE_BEOS))
#       include <arpa/inet.h>
#       if (!defined (TCP_NODELAY))
#           include <netinet/tcp.h>
#       endif
#   endif
#   if (defined (__UTYPE_IBMAIX) || defined(__UTYPE_QNX))
#       include <sys/select.h>
#   endif
#   if (defined (__UTYPE_BEOS))
#       include <NetKit.h>
#   endif
#   if ((defined (_XOPEN_REALTIME) && (_XOPEN_REALTIME >= 1)) \
     || (defined (_POSIX_VERSION)  && (_POSIX_VERSION  >= 199309L)))
#       include <sched.h>
#   endif
#   if (defined (__UTYPE_OSX))
#       include <crt_externs.h>         //  For _NSGetEnviron()
#   endif
#endif

#if (defined (__VMS__))
#   if (!defined (vaxc))
#       include <fcntl.h>               //  Not provided by Vax C
#   endif
#   include <netdb.h>
#   include <unistd.h>
#   include <pthread.h>
#   include <unixio.h>
#   include <unixlib.h>
#   include <types.h>
#   include <file.h>
#   include <socket.h>
#   include <dirent.h>
#   include <time.h>
#   include <pwd.h>
#   include <stat.h>
#   include <in.h>
#   include <inet.h>
#endif

#if (defined (__OS2__))
#   include <sys/types.h>               //  Required near top
#   include <fcntl.h>
#   include <malloc.h>
#   include <netdb.h>
#   include <unistd.h>
#   include <pthread.h>
#   include <dirent.h>
#   include <pwd.h>
#   include <grp.h>
#   include <io.h>
#   include <process.h>
#   include <sys/param.h>
#   include <sys/socket.h>
#   include <sys/select.h>
#   include <sys/time.h>
#   include <sys/stat.h>
#   include <sys/ioctl.h>
#   include <sys/file.h>
#   include <sys/wait.h>
#   include <netinet/in.h>              //  Must come before arpa/inet.h
#   include <arpa/inet.h>
#   include <utime.h>
#   if (!defined (TCP_NODELAY))
#       include <netinet/tcp.h>
#   endif
#endif

//  Add missing defines for Android
#ifdef __UTYPE_ANDROID
#   ifndef S_IREAD
#       define S_IREAD S_IRUSR
#   endif
#   ifndef S_IWRITE
#       define S_IWRITE S_IWUSR
#   endif
#endif


//- Check compiler data type sizes ------------------------------------------

#if (UCHAR_MAX != 0xFF)
#   error "Cannot compile: must change definition of 'byte'."
#endif
#if (USHRT_MAX != 0xFFFFU)
#    error "Cannot compile: must change definition of 'dbyte'."
#endif
#if (UINT_MAX != 0xFFFFFFFFU)
#    error "Cannot compile: must change definition of 'qbyte'."
#endif

//- Data types --------------------------------------------------------------

typedef unsigned char   byte;           //  Single unsigned byte = 8 bits
typedef unsigned short  dbyte;          //  Double byte = 16 bits
typedef unsigned int    qbyte;          //  Quad byte = 32 bits
typedef struct sockaddr_in inaddr_t;    //  Internet socket address structure

//- Inevitable macros -------------------------------------------------------

#define streq(s1,s2)    (!strcmp ((s1), (s2)))
#define strneq(s1,s2)   (strcmp ((s1), (s2)))

//  Provide random number from 0..(num-1)
#if (defined (__WINDOWS__)) || (defined (__UTYPE_IBMAIX)) \
 || (defined (__UTYPE_HPUX)) || (defined (__UTYPE_SUNOS))
#   define randof(num)  (int) ((float) (num) * rand () / (RAND_MAX + 1.0))
#else
#   define randof(num)  (int) ((float) (num) * random () / (RAND_MAX + 1.0))
#endif

// Windows MSVS doesn't have stdbool
#if (defined (__WINDOWS__))
#   if (!defined (__cplusplus) && (!defined (true)))
#       define true 1
#       define false 0
        typedef char bool;
#   endif
#else
#   include <stdbool.h>
#endif

//- A number of POSIX and C99 keywords and data types -----------------------

#if (defined (__WINDOWS__))
#   if (!defined (__cplusplus) && (!defined (inline)))
#       define inline __inline
#   endif
#   define strtoull _strtoui64
#   define srandom srand
#   define TIMEZONE _timezone
#   if (!defined (__MINGW32__))
#       define snprintf _snprintf
#       define vsnprintf _vsnprintf
#   endif
    typedef unsigned long ulong;
    typedef unsigned int  uint;
#   if (!defined (__MINGW32__))
    typedef int mode_t;
    typedef long ssize_t;
    typedef __int32 int32_t;
    typedef __int64 int64_t;
    typedef unsigned __int32 uint32_t;
    typedef unsigned __int64 uint64_t;
#   endif
#   if (!defined (va_copy))
    //  MSVC does not support C99's va_copy so we use a regular assignment
#       define va_copy(dest,src) (dest) = (src)
#   endif
#elif (defined (__APPLE__))
    typedef unsigned long ulong;
    typedef unsigned int uint;
#endif

//- Error reporting ---------------------------------------------------------
// If the compiler is GCC or supports C99, include enclosing function
// in ZMTP assertions
#if defined (__STDC_VERSION__) && (__STDC_VERSION__ >= 199901L)
#   define ZMTP_ASSERT_SANE_FUNCTION    __func__
#elif defined (__GNUC__) && (__GNUC__ >= 2)
#   define ZMTP_ASSERT_SANE_FUNCTION    __FUNCTION__
#else
#   define ZMTP_ASSERT_SANE_FUNCTION    "<unknown>"
#endif

//  Replacement for malloc() which asserts if we run out of heap, and
//  which zeroes the allocated block.
static inline void *
    safe_malloc (
    size_t size,
    const char *file,
    unsigned line,
    const char *func)
{
    void
        *mem;

    mem = calloc (1, size);
    if (mem == NULL) {
        fprintf (stderr, "FATAL ERROR at %s:%u, in %s\n", file, line, func);
        fprintf (stderr, "OUT OF MEMORY (malloc returned NULL)\n");
        fflush (stderr);
        abort ();
    }
    return mem;
}

//  Define _ZMALLOC_DEBUG if you need to trace memory leaks using e.g. mtrace,
//  otherwise all allocations will claim to come from zfl_prelude.h.  For best
//  results, compile all classes so you see dangling object allocations.
//  _ZMALLOC_PEDANTIC does the same thing, but its intention is to propagate
//  out of memory condition back up the call stack.
#if defined _ZMALLOC_DEBUG || _ZMALLOC_PEDANTIC
#   define zmalloc(size) calloc(1,(size))
#else
#   define zmalloc(size) safe_malloc((size), __FILE__, __LINE__, ZMTP_ASSERT_SANE_FUNCTION)
#endif

//- Socket header files -----------------------------------------------------

#if defined (HAVE_LINUX_WIRELESS_H)
#   include <linux/wireless.h>
#else
#   if defined (HAVE_NET_IF_H)
#       include <net/if.h>
#   endif
#   if defined (HAVE_NET_IF_MEDIA_H)
#       include <net/if_media.h>
#   endif
#endif
//  Lets us write code that compiles both on Windows and normal platforms
#if !defined (__WINDOWS__)
typedef int SOCKET;

//#   define closesocket close

#   define INVALID_SOCKET -1
#   define SOCKET_ERROR -1
#endif

//- DLL exports -------------------------------------------------------------

#if defined (__WINDOWS__)
#   if defined LIBZMTP_STATIC
#       define ZMTP_EXPORT
#   elif defined LIBZMTP_EXPORTS
#       define ZMTP_EXPORT __declspec(dllexport)
#   else
#       define ZMTP_EXPORT __declspec(dllimport)
#   endif
#else
#   define ZMTP_EXPORT
#endif

#endif
