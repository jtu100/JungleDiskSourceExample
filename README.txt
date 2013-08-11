Jungle Disk Data Access Example Code v1.51
-----------------------------------------

The included source demonstrates how to retrieve data from Amazon S3 storage that was stored via Jungle Disk.
The code is provided in order to:
1. Provide assurance to Jungle Disk users that no matter what happens, their data will always be available
2. Encourage other developers to create Jungle Disk-compatible utilities that use the same naming conventions and encryption methods

The following functionality is provided:
1. Enumerate the Jungle Disk created buckets for a given AWS account
2. List the directory (and sub-directory) contents of any Jungle Disk bucket
3. Download and decrypt any files stored by Jungle Disk

The code should also provide a useful guide for developers wishing to write S3 files in a Jungle Disk compatible manner, although no PUT example is provided.

The code is provided under the terms of the GNU General Public License.
Developers that wish to use the code for non-GNU licensed software should use it as a reference and re-implement the functionality as needed.

*** Note that the provided code is NOT the actual source code to Jungle Disk, which is not currently available.

To compile, you will need MS Visual Studio 2005. Visual Studio 2003 may also work, although you will need to create a new project file.
The Amazon.com S3 REST example code is included and is required. Only a minor modification has been made to the Amazon code to allow for streaming GET downloads of large objects.

Once compiled, the example provides a command line utility that allows you to browse a Jungle Disk and download data.
The usage is: jdcmd.exe <AccessKeyID> <SecretKeyID> <Command>
Available Commands:
 listbuckets - Displays a list of all available buckets
 dir <bucket> <path> - Displays a list of files in the specified bucket and path
 getfile <bucket> <path> <localfile.ext> - Retrieves the file at the specified bucket and path
 
Examples:
jdcmd.exe 12345 abcdef listbuckets
jdcmd.exe 12345 abcdef dir default /
jdcmd.exe 12345 abcdef getfile default /myfile.txt

