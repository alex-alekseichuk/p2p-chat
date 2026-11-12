<?php

// used in get_param()
function strip($value)
{
  if(get_magic_quotes_gpc() != 0)
  {
    if(is_array($value))  
      foreach($value as $key=>$val)
        $value[$key] = stripslashes($val);
    else
      $value = stripslashes($value);
  }
  return $value;
}

// get param from HTTP
function get_param($parameter_name, $default_value = "")
{
    $parameter_value = "";
    if(isset($_POST[$parameter_name]))
        $parameter_value = strip($_POST[$parameter_name]);
    else if(isset($_GET[$parameter_name]))
        $parameter_value = strip($_GET[$parameter_name]);
    else
        $parameter_value = $default_value;
    return $parameter_value;
}

$winver = get_param("winver", "");
$dotnet1 = get_param("dotnet1", "");
$dotnet2 = get_param("dotnet2", "");
$country = get_param("country", "");
$gmt = get_param("gmt", "");

$f = fopen("/temp/p2p.log", "a");

fputs($f, "winver=" . $winver . ", ");
fputs($f, "dotnet1=" . $dotnet1 . ", ");
fputs($f, "dotnet2=" . $dotnet2 . ", ");
fputs($f, "country=" . $country . ", ");
fputs($f, "gmt=" . $gmt . ", ");
fputs($f, "\n");

?>