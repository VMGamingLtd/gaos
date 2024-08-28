#set -xe


function edit_model_file()
{
  local model_file=$1
  #sed -n '/{[ ]*get[ ]*;[ ]*set;[ ]*}/p' $model_file 
  sed -i  's/[ ]*{[ ]*get[ ]*;[ ]*set[ ]*;[ ]*}[ ]*/;/' $model_file 
}

function get_model_files()
{
  local model_folder_path=$1
  echo $folder_path

  find $model_folder_path  -name "*.cs" -print
}

function edit_model_files()
{
  local model_files=$1

  for model_file in $model_files
  do
    #echo $model_file
    edit_model_file $model_file
  done
}

function edit_model_folder()
{
  local model_folder_path=$1

  local model_files=$(get_model_files $model_folder_path)
  edit_model_files "$model_files"
}

function edit_model_folders()
{
  local model_folders="$*"
  for model_folder in $model_folders
  do
    edit_model_folder $model_folder
  done
}

function copy_models_to()
{
  local root_path=$1

  rm -rf $root_path/Model
  cp -r ../Model $root_path/Model

  rm -rf $root_path/Dbo/Model
  cp -r ../Dbo/Model $root_path/Dbo/Model

  rm -rf $root_path/Routes/Model
  cp -r ../Routes/Model $root_path/Routes/Model

  edit_model_folders \
    $root_path/Model \
    $root_path/Dbo/Model \
    $root_path/Routes/Model
}

function copy_jsondiff()
{
  local root_path=../../gao/Assets/Scripts/Gaos

  rm -rf $root_path/jsondiff
  cp -r ../jsondiff $root_path/jsondiff
}


# copy models to gao
copy_models_to ../../gao/Assets/Scripts/Gaos

copy_jsondiff


