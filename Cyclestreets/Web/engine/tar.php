<?php
/*********************************************************
 * $HeadURL: http://www.maidenfans.com/svn/rwdownload_new/TRUNK/engine/tar.php $
 * $Revision: 50 $
 * $LastChangedBy: realworld $
 * $LastChangedDate: 2008-12-20 20:32:52 +0000 (Sat, 20 Dec 2008) $
 *********************************************************/
/*
+---------------------------------------------------------------------------
|
|   > GNU Tar creation and extraction module
|   > Module written by Matt Mecham
|   > Usage style based on the C and Perl GNU modules
|   > Will only work with PHP 4+
|   
|   > Date started: 15th Feb 2002
|
|	> Module Version Number: 1.0.0
|   > Module Author: Matthew Mecham
+--------------------------------------------------------------------------
|
| LICENCE OF USE (THIS MODULE ONLY)
|
| This module has been created and released under the GNU licence and may be
| freely used and distributed. If you find some space to credit us in your source
| code, it'll be appreciated.
|
| Report all bugs / improvements to matt@ibforums.com
|
| NOTE: This does not affect the current licence for the rest of the Invision
| board code. I just wanted to share this module as there is a lack of other
| decent tar proggies for PHP.
|
+--------------------------------------------------------------------------
*/

/*************************************************************
|
| EXTRACTION USAGE:
|
| $tar = new tar();
| $tar->new_tar("/foo/bar", "myTar.tar");
| $files = $tar->list_files();
| $tar->extract_files( "/extract/to/here/dir" );
|
| CREATION USAGE:
|
| $tar = new tar();
| $tar->new_tar("/foo/bar" , "myNewTar.tar");
| $tar->current_dir("/foo" );  //Optional - tells the script which dir we are in
|                                to syncronise file creation from the tarball
| $tar->add_files( $file_names_with_path_array );
| (or $tar->add_directory( "/foo/bar/myDir" ); to archive a complete dir)
| $tar->write_tar();
|
*************************************************************/



class tar {
	
	var $tar_header_length = '512';
	var $tar_unpack_header = 'a100filename/a8mode/a8uid/a8gid/a12size/a12mtime/a8chksum/a1typeflag/a100linkname/a6magic/a2version/a32uname/a32gname/a8devmajor/a8devminor/a155/prefix';
	var $tar_pack_header   = 'A100 A8 A8 A8 A12 A12 A8 A1 A100 A6 A2 A32 A32 A8 A8 A155';
	var $current_dir       = "";
	var $unpack_dir        = "";
	var $pack_dir          = "";
	var $error             = "";
	var $work_dir          = array();
	var $tar_in_mem        = array();
	var $tar_filename      = "";
	var $filehandle        = "";
	var $warnings          = array();
	var $attributes        = array();
	var $tarfile_name      = "";
	var $tarfile_path      = "";
	var $tarfile_path_name = "";
	var $workfiles         = array();
	
	//-----------------------------------------
	// CONSTRUCTOR: Attempt to guess the current working dir.
	//-----------------------------------------
	
	function tar() {
		global $HTTP_SERVER_VARS;
		
		if ($this_dir = getcwd())
		{
			$this->current_dir = $this_dir;
		}
		else if (isset($HTTP_SERVER_VARS['DOCUMENT_ROOT']))
		{
			$this->current_dir = $HTTP_SERVER_VARS['DOCUMENT_ROOT'];
		}
		else
		{
			$this->current_dir = './';
		}
		
		// Set some attributes, these can be overriden later
		
		$this->attributes = array(  'over_write_existing'   => 0,
								    'over_write_newer'      => 0,
									'remove_tar_file'       => 0,
									'remove_original_files' => 0,
								 );
	}
	
	//-----------------------------------------
	// Set the tarname. If we are extracting a tarball, then it must be the
	// path to the tarball, and it's name (eg: $tar->new_tar("/foo/bar" ,'myTar.tar')
	// or if we are creating a tar, then it must be the path and name of the tar file
	// to create.
	//-----------------------------------------
	
	function new_tar($tarpath, $tarname) {
		
		$this->tarfile_name = $tarname;
		$this->tarfile_path = $tarpath;
		
		// Make sure there isn't a trailing slash on the path
		
		$this->tarfile_path = preg_replace( "#[/\\\]$#" , "" , $this->tarfile_path );
		
		$this->tarfile_path_name = $this->tarfile_path .'/'. $this->tarfile_name; 
		
	}
	
	
	//-----------------------------------------
	// Easy way to overwrite defaults
	//-----------------------------------------
	
	function over_write_existing() {
		$this->attributes['over_write_existing'] = 1;
	}
	function over_write_newer() {
		$this->attributes['over_write_newer'] = 1;
	}
	function remove_tar_file() {
		$this->attributes['remove_tar_file'] = 1;
	}
	function remove_original_files() {
		$this->attributes['remove_original_files'] = 1;
	}
	
	
	
	//-----------------------------------------
	// User assigns the root directory for the tar ball creation/extraction
	//-----------------------------------------
	
	function current_dir($dir = "") {
		
		$this->current_dir = $dir;
		
	}
	
	//-----------------------------------------
	// list files: returns an array with all the filenames in the tar file
	//-----------------------------------------
	
	function list_files($advanced="") {
	
		// $advanced == "" - return name only
		// $advanced == 1  - return name, size, mtime, mode
	
		$data = $this->read_tar();
		
		$final = array();
		
		foreach($data as $d)
		{
			if ($advanced == 1)
			{
				$final[] = array ( 'name'  => $d['name'],
								   'size'  => $d['size'],
								   'mtime' => $d['mtime'],
								   'mode'  => substr(decoct( $d['mode'] ), -4),
								 );
			}
			else
			{
				$final[] = $d['name'];
			}
		}
		
		return $final;
	}
	
	//-----------------------------------------
	// Add a directory to the tar files.
	// $tar->add_directory( str(TO DIRECTORY) )
	//    Can be used in the following methods.
	//	  $tar->add_directory( "/foo/bar" );
	//	  $tar->write_tar( "/foo/bar" );
	//-----------------------------------------
	
	function add_directory( $dir ) {
	
		$this->error = "";
	
		// Make sure the $to_dir is pointing to a valid dir, or we error
		// and return
		
		if (! is_dir($dir) )
		{
			$this->error = "Extract files error: Destination directory ($to_dir) does not exist";
			return FALSE;
		}
		
		$cur_dir = getcwd();
		chdir($dir);
		
		$this->get_dir_contents("./");
		
		$this->add_files($this->workfiles, $dir);
		
		chdir($cur_dir);
		
	}
	
	//-----------------------------------------
	
	function get_dir_contents( $dir )
	{
	
		$dir = preg_replace( "#/$#", "", $dir );
		
		if ( file_exists($dir) )
		{
			if ( is_dir($dir) )
			{
				$handle = opendir($dir);
				
				while (($filename = readdir($handle)) !== false)
				{
					if (($filename != ".") and ($filename != ".."))
					{
						if (is_dir($dir."/".$filename))
						{
							$this->get_dir_contents($dir."/".$filename);
						}
						else
						{
							$this->workfiles[] = $dir."/".$filename;
						}
					}
				}
				
				closedir($handle);
			}
			else
			{
				$this->error = "$dir is not a directory";
				return FALSE;
			}
		}
		else
		{
			$this->error = "Could not locate $dir";
			return;
		}
	}
	
	//-----------------------------------------
	// Extract the tarball
	// $tar->extract_files( str(TO DIRECTORY), [ array( FILENAMES )  ] )
	//    Can be used in the following methods.
	//	  $tar->extract( "/foo/bar" , $files );
	// 	  This will seek out the files in the user array and extract them
	//    $tar->extract( "/foo/bar" );
	//    Will extract the complete tar file into the user specified directory
	//-----------------------------------------
	
	function extract_files( $to_dir, $files="" ) {
	
		$this->error = "";
	
		// Make sure the $to_dir is pointing to a valid dir, or we error
		// and return
		
		if (! is_dir($to_dir) )
		{
			$this->error = "Extract files error: Destination directory ($to_dir) does not exist";
			return;
		}
		
		//-----------------------------------------
		// change into the directory chosen by the user.
		//-----------------------------------------
		
		chdir($to_dir);
		$cur_dir = getcwd();
		
		$to_dir_slash = $to_dir . "/";
		
		//-----------------------------------------
		// Get the file info from the tar
		//-----------------------------------------
		
		$in_files = $this->read_tar();
		
		if ($this->error != "") {
			return;
		}
		
		foreach ($in_files as $k => $file) {
		
			//-----------------------------------------
			// Are we choosing which files to extract?
			//-----------------------------------------
			
			if (is_array($files))
			{
				if (! in_array($file['name'], $files) )
				{
					continue;
				}
			}
			
			chdir($cur_dir);
			
			//-----------------------------------------
			// GNU TAR format dictates that all paths *must* be in the *nix
			// format - if this is not the case, blame the tar vendor, not me!
			//-----------------------------------------
			
			if ( preg_match("#/#", $file['name']) )
			{
				$path_info = explode( "/" , $file['name'] );
				$file_name = array_pop($path_info);
			} else
			{
				$path_info = array();
				$file_name = $file['name'];
			}
			
			//-----------------------------------------
			// If we have a path, then we must build the directory tree
			//-----------------------------------------
			
			
			if (count($path_info) > 0)
			{
				foreach($path_info as $dir_component)
				{
					if ($dir_component == "")
					{
						continue;
					}
					if ( (file_exists($dir_component)) and (! is_dir($dir_component)) )
					{
						$this->warnings[] = "WARNING: $dir_component exists, but is not a directory";
						continue;
					}
					if (! is_dir($dir_component))
					{
						mkdir( $dir_component, 0777);
						chmod( $dir_component, 0777);
					}
					
					if (! @chdir($dir_component))
					{
						$this->warnings[] = "ERROR: CHDIR to $dir_component FAILED!";
					}
				}
			}
			
			//-----------------------------------------
			// check the typeflags, and work accordingly
			//-----------------------------------------
			
			if (($file['typeflag'] == 0) or (!$file['typeflag']) or ($file['typeflag'] == ""))
			{
				if ( $FH = fopen($file_name, "wb") )
				{
					fputs( $FH, $file['data'], strlen($file['data']) );
					fclose($FH);
				}
				else
				{
					$this->warnings[] = "Could not write data to $file_name";
				}
			}
			else if ($file['typeflag'] == 5)
			{
				if ( (file_exists($file_name)) and (! is_dir($file_name)) )
				{
					$this->warnings[] = "$file_name exists, but is not a directory";
					continue;
				}
				if (! is_dir($file_name))
				{
					@mkdir( $file_name, 0777);
				}
			}
			else if ($file['typeflag'] == 6)
			{
				$this->warnings[] = "Cannot handle named pipes";
				continue;
			}
			else if ($file['typeflag'] == 1)
			{
				$this->warnings[] = "Cannot handle system links";
			}
			else if ($file['typeflag'] == 4)
			{
				$this->warnings[] = "Cannot handle device files";
			}	
			else if ($file['typeflag'] == 3)
			{
				$this->warnings[] = "Cannot handle device files";
			}
			else
			{
				$this->warnings[] = "Unknown typeflag found";
			}
			
			if (! @chmod( $file_name, $file['mode'] ) )
			{
				$this->warnings[] = "ERROR: CHMOD $mode on $file_name FAILED!";
			}
			
			@touch( $file_name, $file['mtime'] );
			
		}
		
		// Return to the "real" directory the scripts are in
		
		@chdir($this->current_dir);
		
	}
		
	//-----------------------------------------
	// add files:
	//  Takes an array of files, and adds them to the tar file
	//  Optionally takes a path to use as root for the tar file - if omitted, it
	//  assumes the current working directory is the tarball root. Be careful with
	//  this, or you may get unexpected results -especially when attempting to read
	//  and add files to the tarball.
	//  EXAMPLE DIR STRUCTURE
	//  /usr/home/somedir/forums/sources
	//  BRIEF: To tar up the sources directory
	// $files = array( 'sources/somescript.php', 'sources/anothersscript.php' );
	//  If CWD is 'somedir', you'll need to use $tar->add_files( $files, "/usr/home/somedir/forums" );
	//  or it'll attempt to open /usr/home/somedir/sources/somescript.php - which would result
	//  in an error. Either that, or use:
	//  chdir("/usr/home/somedir/forums");
	//  $tar->add_files( $files );
	//-----------------------------------------
	
	function add_files( $files, $root_path="" )
	{
		// Do we a root path to change into?
		
		if ($root_path != "")
		{
			chdir($root_path);
		}
		
		$count    = 0;
		
		foreach ($files as $file)
		{
			// is it a Mac OS X work file?
			
			if ( preg_match("/\.ds_store/i", $file ) )
			{
				continue;
			}
		
			$typeflag = 0;
			$data     = "";
			$linkname = "";
			
			$stat = stat($file);
			
			// Did stat fail?
			
			if (! is_array($stat) )
			{
				$this->warnings[] = "Error: Stat failed on $file";
				continue;
			}
			
			$mode  = fileperms($file);
			$uid   = $stat[4];
			$gid   = $stat[5];
			$rdev  = $stat[6];
			$size  = filesize($file);
			$mtime = filemtime($file);
			
			if (is_file($file))
			{
				// It's a plain file, so lets suck it up
				
				$typeflag = 0;
				
				if ( $FH = fopen($file, 'rb') )
				{
					$data = fread( $FH, filesize($file) );
					fclose($FH);
				}
				else
				{
					$this->warnings[] = "ERROR: Failed to open $file";
					continue;
				}
			}
			else if (is_link($file))
			{
				$typeflag = 1;
				$linkname = @readlink($file);
			}
			else if (is_dir($file))
			{
				$typeflag = 5;
			}
			else
			{
				// Sockets, Pipes and char/block specials are not
				// supported, so - lets use a silly value to keep the
				// tar ball legitimate.
				$typeflag = 9;
			}
			
			// Add this data to our in memory tar file
			
			$this->tar_in_mem[] = array (
										  'name'     => $file,
										  'mode'     => $mode,
										  'uid'      => $uid,
										  'gid'      => $gid,
										  'size'     => strlen($data),
										  'mtime'    => $mtime,
										  'chksum'   => "      ",
										  'typeflag' => $typeflag,
										  'linkname' => $linkname,
										  'magic'    => "ustar\0",
										  'version'  => '00',
										  'uname'    => 'unknown',
										  'gname'    => 'unknown',
										  'devmajor' => "",
										  'devminor' => "",
										  'prefix'   => "",
										  'data'     => $data
										);
			// Clear the stat cache
			
			@clearstatcache();
			
			$count++;
		}
		
		@chdir($this->current_dir);
		
		//Return the number of files to anyone who's interested
		
		return $count;
	
	}
	
	//-----------------------------------------
	// write_tar:
	// Writes the tarball into the directory specified in new_tar with a filename
	// specified in new_tar
	//-----------------------------------------
	
	function write_tar() {
	
		if ($this->tarfile_path_name == "") {
			$this->error = 'No filename or path was specified to create a new tar file';
			return;
		}
		
		if ( count($this->tar_in_mem) < 1 ) {
			$this->error = 'No data to write to the new tar file';
			return;
		}
		
		$tardata = "";
		
		foreach ($this->tar_in_mem as $file) {
		
			$prefix = "";
			$tmp    = "";
			$last   = "";
		
			// make sure the filename isn't longer than 99 characters.
			
			if (strlen($file['name']) > 99)
			{
				$pos = strrpos( $file['name'], "/" );
				
				if (is_string($pos) and !$pos)
				{
					// filename alone is longer than 99 characters!
					$this->error[] = "Filename {$file['name']} exceeds the length allowed by GNU Tape ARchives";
					continue;
				}
				
				$prefix = substr( $file['name'], 0 , $pos );  // Move the path to the prefix
				$file['name'] = substr( $file['name'], ($pos+1));
				
				if (strlen($prefix) > 154)
				{
					$this->error[] = "File path exceeds the length allowed by GNU Tape ARchives";
					continue;
				}
			}
			
			// BEGIN FORMATTING (a8a1a100)
			
			$mode  = sprintf("%6s ", decoct($file['mode']));
			$uid   = sprintf("%6s ", decoct($file['uid']));
			$gid   = sprintf("%6s ", decoct($file['gid']));
			$size  = sprintf("%11s ", decoct($file['size']));
			$mtime = sprintf("%11s ", decoct($file['mtime']));
			
			$tmp  = pack("a100a8a8a8a12a12",$file['name'],$mode,$uid,$gid,$size,$mtime);
						
			$last  = pack("a1"   , $file['typeflag']);
			$last .= pack("a100" , $file['linkname']);
								
			$last .= pack("a6", "ustar"); // magic
			$last .= pack("a2", "" ); // version
			$last .= pack("a32", $file['uname']);
			$last .= pack("a32", $file['gname']);
			$last .= pack("a8", ""); // devmajor
			$last .= pack("a8", ""); // devminor
			$last .= pack("a155", $prefix);
			//$last .= pack("a12", "");
			$test_len = $tmp . $last . "12345678";
			$last .= $this->internal_build_string( "\0" , ($this->tar_header_length - strlen($test_len)) );
			
			// Here comes the science bit, handling
			// the checksum.
			
			$checksum = 0;
			
			for ($i = 0 ; $i < 148 ; $i++ )
			{
				$checksum += ord( substr($tmp, $i, 1) );
			}
			
			for ($i = 148 ; $i < 156 ; $i++)
			{
				$checksum += ord(' ');
			}
			
			for ($i = 156, $j = 0 ; $i < 512 ; $i++, $j++)
			{
				$checksum += ord( substr($last, $j, 1) );
			}
			
			$checksum = sprintf( "%6s ", decoct($checksum) );
			
			$tmp .= pack("a8", $checksum);
			
			$tmp .= $last;
		   	
		   	$tmp .= $file['data'];
		   	
		   	// Tidy up this chunk to the power of 512
		   	
		   	if ($file['size'] > 0)
		   	{
		   		if ($file['size'] % 512 != 0)
		   		{
		   			$homer = $this->internal_build_string( "\0" , (512 - ($file['size'] % 512)) );
		   			$tmp .= $homer;
		   		}
		   	}
		   	
		   	$tardata .= $tmp;
		}
		
		// Add the footer
		
		$tardata .= pack( "a512", "" );
		
		// print it to the tar file
		
		$FH = fopen( $this->tarfile_path_name, 'wb' );
		fputs( $FH, $tardata, strlen($tardata) );
		fclose($FH);
		
		@chmod( $this->tarfile_path_name, 0777);
		
		// Done..
	}
		   
	//-----------------------------------------
	// Read the tarball - builds an associative array
	//-----------------------------------------
	
	function read_tar() {
	
		$filename = $this->tarfile_path_name;
	
		if ($filename == "") {
			$this->error = 'No filename specified when attempting to read a tar file';
			return array();
		}
		
		if (! file_exists($filename) ) {
			$this->error = 'Cannot locate the file '.$filename;
			return array();
		}

		$tar_info = array();
		
		$this->tar_filename = $filename;
		
		// Open up the tar file and start the loop

		if (! $FH = fopen( $filename , 'rb' ) ) {
			$this->error = "Cannot open $filename for reading";
			return array();
		}
		
		// Grrr, perl allows spaces, PHP doesn't. Pack strings are hard to read without
		// them, so to save my sanity, I'll create them with spaces and remove them here
		
		$this->tar_unpack_header = preg_replace( "/\s/", "" , $this->tar_unpack_header);
		
		while (!feof($FH)) {
		
			$buffer = fread( $FH , $this->tar_header_length );
			
			// check the block
			
			$checksum = 0;
			
			for ($i = 0 ; $i < 148 ; $i++) {
				$checksum += ord( substr($buffer, $i, 1) );
			}
			for ($i = 148 ; $i < 156 ; $i++) {
				$checksum += ord(' ');
			}
			for ($i = 156 ; $i < 512 ; $i++) {
				$checksum += ord( substr($buffer, $i, 1) );
			}
			
			$fa = unpack( $this->tar_unpack_header, $buffer);

			$name     = trim($fa[filename]);
			$mode     = OctDec(trim($fa[mode]));
			$uid      = OctDec(trim($fa[uid]));
			$gid      = OctDec(trim($fa[gid]));
			$size     = OctDec(trim($fa[size]));
			$mtime    = OctDec(trim($fa[mtime]));
			$chksum   = OctDec(trim($fa[chksum]));
			$typeflag = trim($fa[typeflag]);
			$linkname = trim($fa[linkname]);
			$magic    = trim($fa[magic]);
			$version  = trim($fa[version]);
			$uname    = trim($fa[uname]);
			$gname    = trim($fa[gname]);
			$devmajor = OctDec(trim($fa[devmajor]));
			$devminor = OctDec(trim($fa[devminor]));
			$prefix   = trim($fa[prefix]);
			
			if ( ($checksum == 256) and ($chksum == 0) ) {
				//EOF!
				break;
			}
			
			if ($prefix) {
				$name = $prefix.'/'.$name;
			}
			
			// Some broken tars don't set the type flag
			// correctly for directories, so we assume that
			// if it ends in / it's a directory...
			
			if ( (preg_match( "#/$#" , $name)) and (! $name) ) {
				$typeflag = 5;
			}
			
			// If it's the end of the tarball...
			$test = $this->internal_build_string( '\0' , 512 );
			if ($buffer == $test) {
				break;
			}
			
			// Read the next chunk

			$data = fread( $FH, $size );
			
			if (strlen($data) != $size) {
				$this->error = "Read error on tar file";
				fclose( $FH );
				return array();
			}
			
			$diff = $size % 512;
			
			if ($diff != 0) {
				// Padding, throw away
				$crap = fread( $FH, (512-$diff) );
			}
			
			// Protect against tarfiles with garbage at the end
			
			if ($name == "") {
				break;
			}
			
			$tar_info[] = array (
								  'name'     => $name,
								  'mode'     => $mode,
								  'uid'      => $uid,
								  'gid'      => $gid,
								  'size'     => $size,
								  'mtime'    => $mtime,
								  'chksum'   => $chksum,
								  'typeflag' => $typeflag,
								  'linkname' => $linkname,
								  'magic'    => $magic,
								  'version'  => $version,
								  'uname'    => $uname,
								  'gname'    => $gname,
								  'devmajor' => $devmajor,
								  'devminor' => $devminor,
								  'prefix'   => $prefix,
								  'data'     => $data
								 );
		}
		
		fclose($FH);
		
		return $tar_info;
	}
			





//-----------------------------------------
// INTERNAL FUNCTIONS - These should NOT be called outside this module
//+------------------------------------------------------------------------------
	
	//-----------------------------------------
	// build_string: Builds a repititive string
	//-----------------------------------------
	
	function internal_build_string($string="", $times=0) {
	
		$return = "";
		for ($i=0 ; $i < $times ; ++$i ) {
			$return .= $string;
		}
		
		return $return;
	}
	
	
	
	
	
}

class archive
{
    function archive($name)
    {
        $this->options = array (
            'basedir' => ".",
            'name' => $name,
            'prepend' => "",
            'inmemory' => 0,
            'overwrite' => 0,
            'recurse' => 1,
            'storepaths' => 1,
            'followlinks' => 0,
            'level' => 3,
            'method' => 1,
            'sfx' => "",
            'type' => "",
            'comment' => ""
        );
        $this->files = array ();
        $this->exclude = array ();
        $this->storeonly = array ();
        $this->error = array ();
    }

    function set_options($options)
    {
        foreach ($options as $key => $value)
            $this->options[$key] = $value;
        if (!empty ($this->options['basedir']))
        {
            $this->options['basedir'] = str_replace("\\", "/", $this->options['basedir']);
            $this->options['basedir'] = preg_replace("/\/+/", "/", $this->options['basedir']);
            $this->options['basedir'] = preg_replace("/\/$/", "", $this->options['basedir']);
        }
        if (!empty ($this->options['name']))
        {
            $this->options['name'] = str_replace("\\", "/", $this->options['name']);
            $this->options['name'] = preg_replace("/\/+/", "/", $this->options['name']);
        }
        if (!empty ($this->options['prepend']))
        {
            $this->options['prepend'] = str_replace("\\", "/", $this->options['prepend']);
            $this->options['prepend'] = preg_replace("/^(\.*\/+)+/", "", $this->options['prepend']);
            $this->options['prepend'] = preg_replace("/\/+/", "/", $this->options['prepend']);
            $this->options['prepend'] = preg_replace("/\/$/", "", $this->options['prepend']) . "/";
        }
    }

    function create_archive()
    {
        $this->make_list();

        if ($this->options['inmemory'] == 0)
        {
            $pwd = getcwd();
            chdir($this->options['basedir']);
            if ($this->options['overwrite'] == 0 && file_exists($this->options['name'] . ($this->options['type'] == "gzip" || $this->options['type'] == "bzip" ? ".tmp" : "")))
            {
                $this->error[] = "File {$this->options['name']} already exists.";
                chdir($pwd);
                return 0;
            }
            else if ($this->archive = @fopen($this->options['name'] . ($this->options['type'] == "gzip" || $this->options['type'] == "bzip" ? ".tmp" : ""), "wb+"))
                chdir($pwd);
            else
            {
                $this->error[] = "Could not open {$this->options['name']} for writing.";
                chdir($pwd);
                return 0;
            }
        }
        else
            $this->archive = "";

        switch ($this->options['type'])
        {
        case "zip":
            if (!$this->create_zip())
            {
                $this->error[] = "Could not create zip file.";
                return 0;
            }
            break;
        case "bzip":
            if (!$this->create_tar())
            {
                $this->error[] = "Could not create tar file.";
                return 0;
            }
            if (!$this->create_bzip())
            {
                $this->error[] = "Could not create bzip2 file.";
                return 0;
            }
            break;
        case "gzip":
            if (!$this->create_tar())
            {
                $this->error[] = "Could not create tar file.";
                return 0;
            }
            if (!$this->create_gzip())
            {
                $this->error[] = "Could not create gzip file.";
                return 0;
            }
            break;
        case "tar":
            if (!$this->create_tar())
            {
                $this->error[] = "Could not create tar file.";
                return 0;
            }
        }

        if ($this->options['inmemory'] == 0)
        {
            fclose($this->archive);
            if ($this->options['type'] == "gzip" || $this->options['type'] == "bzip")
                unlink($this->options['basedir'] . "/" . $this->options['name'] . ".tmp");
        }
    }

    function add_data($data)
    {
        if ($this->options['inmemory'] == 0)
            fwrite($this->archive, $data);
        else
            $this->archive .= $data;
    }

    function make_list()
    {
        if (!empty ($this->exclude))
            foreach ($this->files as $key => $value)
                foreach ($this->exclude as $current)
                    if ($value['name'] == $current['name'])
                        unset ($this->files[$key]);
        if (!empty ($this->storeonly))
            foreach ($this->files as $key => $value)
                foreach ($this->storeonly as $current)
                    if ($value['name'] == $current['name'])
                        $this->files[$key]['method'] = 0;
        unset ($this->exclude, $this->storeonly);
    }

    function add_files($list)
    {
        $temp = $this->list_files($list);
        foreach ($temp as $current)
            $this->files[] = $current;
    }

    function exclude_files($list)
    {
        $temp = $this->list_files($list);
        foreach ($temp as $current)
            $this->exclude[] = $current;
    }

    function store_files($list)
    {
        $temp = $this->list_files($list);
        foreach ($temp as $current)
            $this->storeonly[] = $current;
    }

    function list_files($list)
    {
        if (!is_array ($list))
        {
            $temp = $list;
            $list = array ($temp);
            unset ($temp);
        }

        $files = array ();

        $pwd = getcwd();
        chdir($this->options['basedir']);

        foreach ($list as $current)
        {
            $current = str_replace("\\", "/", $current);
            $current = preg_replace("/\/+/", "/", $current);
            $current = preg_replace("/\/$/", "", $current);
            if (strstr($current, "*"))
            {
                $regex = preg_replace("/([\\\^\$\.\[\]\|\(\)\?\+\{\}\/])/", "\\\\\\1", $current);
                $regex = str_replace("*", ".*", $regex);
                $dir = strstr($current, "/") ? substr($current, 0, strrpos($current, "/")) : ".";
                $temp = $this->parse_dir($dir);
                foreach ($temp as $current2)
                    if (preg_match("/^{$regex}$/i", $current2['name']))
                        $files[] = $current2;
                unset ($regex, $dir, $temp, $current);
            }
            else if (@is_dir($current))
            {
                $temp = $this->parse_dir($current);
                foreach ($temp as $file)
                    $files[] = $file;
                unset ($temp, $file);
            }
            else if (@file_exists($current))
                $files[] = array ('name' => $current, 'name2' => $this->options['prepend'] .
                    preg_replace("/(\.+\/+)+/", "", ($this->options['storepaths'] == 0 && strstr($current, "/")) ?
                    substr($current, strrpos($current, "/") + 1) : $current),
                    'type' => @is_link($current) && $this->options['followlinks'] == 0 ? 2 : 0,
                    'ext' => substr($current, strrpos($current, ".")), 'stat' => stat($current));
        }

        chdir($pwd);

        unset ($current, $pwd);

        usort($files, array ("archive", "sort_files"));

        return $files;
    }

    function parse_dir($dirname)
    {
        if ($this->options['storepaths'] == 1 && !preg_match("/^(\.+\/*)+$/", $dirname))
            $files = array (array ('name' => $dirname, 'name2' => $this->options['prepend'] .
                preg_replace("/(\.+\/+)+/", "", ($this->options['storepaths'] == 0 && strstr($dirname, "/")) ?
                substr($dirname, strrpos($dirname, "/") + 1) : $dirname), 'type' => 5, 'stat' => stat($dirname)));
        else
            $files = array ();
        $dir = @opendir($dirname);

        while ($file = @readdir($dir))
        {
            $fullname = $dirname . "/" . $file;
            if ($file == "." || $file == "..")
                continue;
            else if (@is_dir($fullname))
            {
                if (empty ($this->options['recurse']))
                    continue;
                $temp = $this->parse_dir($fullname);
                foreach ($temp as $file2)
                    $files[] = $file2;
            }
            else if (@file_exists($fullname))
                $files[] = array ('name' => $fullname, 'name2' => $this->options['prepend'] .
                    preg_replace("/(\.+\/+)+/", "", ($this->options['storepaths'] == 0 && strstr($fullname, "/")) ?
                    substr($fullname, strrpos($fullname, "/") + 1) : $fullname),
                    'type' => @is_link($fullname) && $this->options['followlinks'] == 0 ? 2 : 0,
                    'ext' => substr($file, strrpos($file, ".")), 'stat' => stat($fullname));
        }

        @closedir($dir);

        return $files;
    }

    function sort_files($a, $b)
    {
        if ($a['type'] != $b['type'])
            if ($a['type'] == 5 || $b['type'] == 2)
                return -1;
            else if ($a['type'] == 2 || $b['type'] == 5)
                return 1;
        else if ($a['type'] == 5)
            return strcmp(strtolower($a['name']), strtolower($b['name']));
        else if ($a['ext'] != $b['ext'])
            return strcmp($a['ext'], $b['ext']);
        else if ($a['stat'][7] != $b['stat'][7])
            return $a['stat'][7] > $b['stat'][7] ? -1 : 1;
        else
            return strcmp(strtolower($a['name']), strtolower($b['name']));
        return 0;
    }

    function download_file()
    {
        if ($this->options['inmemory'] == 0)
        {
            $this->error[] = "Can only use download_file() if archive is in memory. Redirect to file otherwise, it is faster.";
            return;
        }
        switch ($this->options['type'])
        {
        case "zip":
            header("Content-Type: application/zip");
            break;
        case "bzip":
            header("Content-Type: application/x-bzip2");
            break;
        case "gzip":
            header("Content-Type: application/x-gzip");
            break;
        case "tar":
            header("Content-Type: application/x-tar");
        }
        $header = "Content-Disposition: attachment; filename=\"";
        $header .= strstr($this->options['name'], "/") ? substr($this->options['name'], strrpos($this->options['name'], "/") + 1) : $this->options['name'];
        $header .= "\"";
        header($header);
        header("Content-Length: " . strlen($this->archive));
        header("Content-Transfer-Encoding: binary");
        header("Cache-Control: no-cache, must-revalidate, max-age=60");
        header("Expires: Sat, 01 Jan 2000 12:00:00 GMT");
        print($this->archive);
    }
}

class tar_file extends archive
{
    function tar_file($name)
    {
        $this->archive($name);
        $this->options['type'] = "tar";
    }

    function create_tar()
    {
        $pwd = getcwd();
        chdir($this->options['basedir']);

        foreach ($this->files as $current)
        {
            if ($current['name'] == $this->options['name'])
                continue;
            if (strlen($current['name2']) > 99)
            {
                $path = substr($current['name2'], 0, strpos($current['name2'], "/", strlen($current['name2']) - 100) + 1);
                $current['name2'] = substr($current['name2'], strlen($path));
                if (strlen($path) > 154 || strlen($current['name2']) > 99)
                {
                    $this->error[] = "Could not add {$path}{$current['name2']} to archive because the filename is too long.";
                    continue;
                }
            }
            $block = pack("a100a8a8a8a12a12a8a1a100a6a2a32a32a8a8a155a12", $current['name2'], sprintf("%07o",
                $current['stat'][2]), sprintf("%07o", $current['stat'][4]), sprintf("%07o", $current['stat'][5]),
                sprintf("%011o", $current['type'] == 2 ? 0 : $current['stat'][7]), sprintf("%011o", $current['stat'][9]),
                "        ", $current['type'], $current['type'] == 2 ? @readlink($current['name']) : "", "ustar ", " ",
                "Unknown", "Unknown", "", "", !empty ($path) ? $path : "", "");

            $checksum = 0;
            for ($i = 0; $i < 512; $i++)
                $checksum += ord(substr($block, $i, 1));
            $checksum = pack("a8", sprintf("%07o", $checksum));
            $block = substr_replace($block, $checksum, 148, 8);

            if ($current['type'] == 2 || $current['stat'][7] == 0)
                $this->add_data($block);
            else if ($fp = @fopen($current['name'], "rb"))
            {
                $this->add_data($block);
                while ($temp = fread($fp, 1048576))
                    $this->add_data($temp);
                if ($current['stat'][7] % 512 > 0)
                {
                    $temp = "";
                    for ($i = 0; $i < 512 - $current['stat'][7] % 512; $i++)
                        $temp .= "\0";
                    $this->add_data($temp);
                }
                fclose($fp);
            }
            else
                $this->error[] = "Could not open file {$current['name']} for reading. It was not added.";
        }

        $this->add_data(pack("a1024", ""));

        chdir($pwd);

        return 1;
    }

    function extract_files($return_path)
    {
        $pwd = getcwd();
        chdir($this->options['basedir']);

        if ($fp = $this->open_archive())
        {
            if ($this->options['inmemory'] == 1)
                $this->files = array ();

            while ($block = fread($fp, 512))
            {
                $temp = unpack("a100name/a8mode/a8uid/a8gid/a12size/a12mtime/a8checksum/a1type/a100symlink/a6magic/a2temp/a32temp/a32temp/a8temp/a8temp/a155prefix/a12temp", $block);
                $file = array (
                    'name' => $temp['prefix'] . $temp['name'],
                    'stat' => array (
                        2 => $temp['mode'],
                        4 => octdec($temp['uid']),
                        5 => octdec($temp['gid']),
                        7 => octdec($temp['size']),
                        9 => octdec($temp['mtime']),
                    ),
                    'checksum' => octdec($temp['checksum']),
                    'type' => $temp['type'],
                    'magic' => $temp['magic'],
                );
                if ($file['checksum'] == 0x00000000)
                    break;
                else if (substr($file['magic'], 0, 5) != "ustar")
                {
                    $this->error[] = "This script does not support extracting this type of tar file.";
                    break;
                }
                $block = substr_replace($block, "        ", 148, 8);
                $checksum = 0;
                for ($i = 0; $i < 512; $i++)
                    $checksum += ord(substr($block, $i, 1));
                if ($file['checksum'] != $checksum)
                    $this->error[] = "Could not extract from {$this->options['name']}, it is corrupt.";

                $listing = explode("/", $file['name']);
                $size = count($listing);
                if ( $size > 1 )
                {
                    for ( $i=0; $i<$size-1; $i++)
                    {
                        $path = "";
                        for ( $j=0; $j<=$i; $j++ )
                        {
                            if ( $j > 0 )
                                $path .= "/";
                            $path .= $listing[$j];
                        }
                        if (!is_dir($path))
                        {
                            mkdir($path, 755);
                            ob_start();
                            $this->make_file_writable($path, $return_path);
                            $content = ob_get_contents();
                            ob_end_clean();
                            if ( $content )
                            {
                                echo $content;
                                $this->error[] = $content;
                                return false;
                            }
                        }
                    }
                    //$file['name'] = $listing[$size-1];
                }
                if ($this->options['inmemory'] == 1)
                {
                    $file['data'] = fread($fp, $file['stat'][7]);
                    fread($fp, (512 - $file['stat'][7] % 512) == 512 ? 0 : (512 - $file['stat'][7] % 512));
                    unset ($file['checksum'], $file['magic']);
                    $this->files[] = $file;
                }
                else if ($file['type'] == 5)
                {
                    if (!is_dir($file['name']))
                    {
                        mkdir($file['name'], 755);
                        ob_start();
                        $this->make_file_writable($file['name'], $return_path);
                        $content = ob_get_contents();
                        ob_end_clean();
                        if ( $content )
                        {
                            echo $content;
                            $this->error[] = $content;
                            return false;
                        }
                    }
                }
                else if ($this->options['overwrite'] == 0 && file_exists($file['name']))
                {
                    $this->error[] = "{$file['name']} already exists.";
                    continue;
                }
                else if ($file['type'] == 2)
                {
                    //symlink($temp['symlink'], $file['name']);
                    //chmod($file['name'], $file['stat'][2]);
                    echo "Found a symlink. Whats THAT doing there!?";
                }
                else if ($new = @fopen($file['name'], "wb"))
                {
                    fwrite($new, fread($fp, $file['stat'][7]));
                    @fread($fp, (512 - $file['stat'][7] % 512) == 512 ? 0 : (512 - $file['stat'][7] % 512));
                    fclose($new);
                    ob_start();
                    $this->make_file_writable($file['name'], $return_path);
                    $content = ob_get_contents();
                    ob_end_clean();
                    if ( $content )
                    {
                        echo $content;
                        $this->error[] = $content;
                        return false;
                    }
                }
                else
                {
                    $this->error[] = "Could not open {$file['name']} for writing.";
                    continue;
                }
                //@chown($file['name'], $file['stat'][4]);
                //@chgrp($file['name'], $file['stat'][5]);
                //touch($file['name'], $file['stat'][9]);
                unset ($file);
            }
        }
        else
            $this->error[] = "Could not open file {$this->options['name']}";

        chdir($pwd);
        //return false;
    }

    function make_file_writable($filename, $path)
    {
        global $g_ftp;

        $failed = false;

        // Linux, check if file is writeable...
        if (substr(__FILE__, 1, 2) != ':\\')
        {
            if (!is_writable($filename) || !is_readable($filename))
            {
                // try 755 first because of suExec PHP
                @chmod($filename, 0755);

                // If 755 didn't work, try 777.
                if ( !is_writable($filename) || !is_readable($filename) )
                {
                    if ( !@chmod($filename, 0777) )
                    {
                        $failed = true;
                    }
                }
            }
        }
        // Fall back for windows...
        else
        {
            // Folders can't be opened for write... but the index.htm in them can
            if ( is_dir($filename) && is_file($filename."/index.php") )
            {
                $filename .= '/index.php';

                @chmod($filename, 0777);
                $fp = @fopen($filename, 'r+');
                if (!$fp)
                    $fp = @fopen($filename, 'w');
                if (!$fp)
                    $failed = true;
                else
                    @fclose($fp);
            }
        }

        if (!$failed)
            return "";

        // We've failed to chmod for some reason...
        // It's not going to be possible to use FTP on windows to solve the problem...
        if (substr(__FILE__, 1, 2) == ':\\')
        {
            echo "Could not make $filename writeable. This file should be writeable for the software to function correctly.";

            return false;
        }
        // Try to fix this with FTP
        else
        {
            //$myFile = "testFile.txt";
            //$fh = fopen($myFile, 'a');

            // Load any session data we might have...
            if (!isset($_POST['ftp_username']) && isset($_SESSION['temp_ftp']))
            {
                $_POST['ftp_server'] = $_SESSION['temp_ftp']['server'];
                $_POST['ftp_port'] = $_SESSION['temp_ftp']['port'];
                $_POST['ftp_username'] = $_SESSION['temp_ftp']['username'];
                $_POST['ftp_password'] = $_SESSION['temp_ftp']['password'];
                $_POST['ftp_path'] = $_SESSION['temp_ftp']['path'];
            }

            if (isset($_POST['ftp_username']) && $g_ftp==NULL)
            {
                $g_ftp = new ClsFTP($_POST['ftp_username'], $_POST['ftp_password'], $_POST['ftp_server'], $_POST['ftp_port']);
                //fwrite($fh, "New FTP Session");
            }
            else
            {
                //fwrite($fh, "already have FTP session");
            }

            if ($g_ftp == NULL)
            {
                echo "<p>We need to set the permissions on the files the installer has just downloaded. This couldn't be done
                because of some restrictions on your server. We can attempt to do this with FTP. The FTP details are only stored for the
                length of the installer. We will <b>not</b> save these details anywhere for security.<br>
                Failure to set the permissions on your files could result in problems with uploads, skinning and some other parts of the script.</p>";
                echo "<form method='post' action='$path'>";
                new_table();
                new_row(-1, "", "", "250");
                    echo "FTP Server:";
                    new_col();
                    echo "<input name='ftp_server' type='text' size='50' value='' />";
                new_row();
                      echo "FTP Port:";
                      new_col();
                      echo "<input name='ftp_port' type='text' size='50' value='21' />";
                new_row();
                      echo "FTP Username:";
                      new_col();
                      echo "<input name='ftp_username' type='text' size='50' value='' />";
                new_row();
                      echo "FTP Password:";
                      new_col();
                      echo "<input name='ftp_password' type='password' size='50' value='' />";
                new_row();
                      echo "FTP path to directory containing this script:";
                      new_col();
                      echo "<input name='ftp_path' type='text' size='50' value='".getpath()."' />";
                end_table();
                echo "<input type='submit'>";
                echo "</form>";

                return false;
            }
            else
            {
                $_SESSION['temp_ftp'] = array(
                    'server' => $_POST['ftp_server'],
                    'port' => $_POST['ftp_port'],
                    'username' => $_POST['ftp_username'],
                    'password' => $_POST['ftp_password'],
                    'path' => $_POST['ftp_path']
                );

                if (!is_writable($filename))
                    $g_ftp->chmod($filename, 0755);
                if (!is_writable($filename))
                    $g_ftp->chmod($filename, 0777);

                //fwrite($fh, "Writing {$filename}");
            }
            //fclose($fh);
        }
        return true;
    }

    function open_archive()
    {
        return @fopen($this->options['name'], "rb");
    }
}

class gzip_file extends tar_file
{
    function gzip_file($name)
    {
        $this->tar_file($name);
        $this->options['type'] = "gzip";
    }

    function create_gzip()
    {
        if ($this->options['inmemory'] == 0)
        {
            $pwd = getcwd();
            chdir($this->options['basedir']);
            if ($fp = gzopen($this->options['name'], "wb{$this->options['level']}"))
            {
                fseek($this->archive, 0);
                while ($temp = fread($this->archive, 1048576))
                    gzwrite($fp, $temp);
                gzclose($fp);
                chdir($pwd);
            }
            else
            {
                $this->error[] = "Could not open {$this->options['name']} for writing.";
                chdir($pwd);
                return 0;
            }
        }
        else
            $this->archive = gzencode($this->archive, $this->options['level']);

        return 1;
    }

    function open_archive()
    {
        return @gzopen($this->options['name'], "rb");
    }
}
?>